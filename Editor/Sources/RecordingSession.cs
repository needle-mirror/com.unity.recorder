using System;
using System.ComponentModel;
using Unity.Profiling;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.LowLevel;

namespace UnityEditor.Recorder
{
    /// <summary>
    /// A class that represents a Recorder session, with support for processing incoming data and preparing as
    /// well as cleaning up resources.
    /// </summary>
    public class RecordingSession : IDisposable
    {
        /// <summary>
        /// The Recorder associated with this session.
        /// </summary>
        public Recorder recorder;

        internal GameObject recorderGameObject;
        internal _FrameRequestComponent recorderComponent;
        static bool frameRateCapped; // Used to have only 1 Recorder cap framerate when multiple are trying to do.
        Unity.Profiling.ProfilerMarker capFPSMarker = new ProfilerMarker("Unity.Recorder.CapFPS");

        int m_SubFrameIndex = 0;
        int m_FrameIndex = 0;
        int m_InitialFrame = 0;
        int m_FirstRecordedFrameCount = -1;
        float m_FPSTimeStart;
        float m_FPSNextTimeStart;
        int m_FPSNextFrameCount;

        internal double currentFrameStartTS { get; private set; }
        internal double recordingStartTS { get; private set; }

        internal DateTime sessionStartTS { get; private set; }

        /// <summary>
        /// The settings of the Recorder.
        /// </summary>
        /// <seealso cref="AnimationRecorderSettings"/>
        /// <seealso cref="FrameCapturer.BaseFCRecorderSettings"/>
        /// <seealso cref="AudioRecorderSettings"/>
        /// <seealso cref="ImageRecorderSettings"/>
        /// <seealso cref="MovieRecorderSettings"/>
        /// <seealso cref="AudioRecorderSettings"/>
        public RecorderSettings settings
        {
            get { return recorder.settings; }
        }

        internal bool isRecording
        {
            get { return recorder.Recording; }
        }

        /// <summary>
        /// The index of the current frame being processed.
        /// </summary>
        public int frameIndex
        {
            get { return m_FrameIndex; }
        }

        /// <summary>
        /// The index of the current sub frame being processed.
        /// </summary>
        internal int subFrameIndex
        {
            get { return m_SubFrameIndex; }
        }


        internal int RecordedFrameSpan
        {
            get { return m_FirstRecordedFrameCount == -1 ? 0 : Time.renderedFrameCount - m_FirstRecordedFrameCount; }
        }

        /// <summary>
        /// The time (in seconds) of the current frame.
        /// </summary>
        public float recorderTime
        {
            get { return (float)(currentFrameStartTS - settings.StartTime); }
        }

        static void AllowInBackgroundMode()
        {
            if (!Application.runInBackground)
            {
                Application.runInBackground = true;
                if (RecorderOptions.VerboseMode)
                    Debug.Log("Recording sessions is enabling Application.runInBackground!");
            }
        }

        bool ValidateFrameRate(float frameRate)
        {
            if (frameRate == 0F)
            {
                Debug.LogErrorFormat("{0} does not support 0 fps frame rate.", recorder.GetType().Name);
                return false;
            }

            if (frameRate < 0F)
            {
                Debug.LogErrorFormat(
                    "{0} does not support negative frame rate ({1}).", recorder.GetType().Name, frameRate);
                return false;
            }

            if (float.IsNaN(frameRate) || float.IsInfinity(frameRate))
            {
                Debug.LogErrorFormat(
                    "{0} does not support invalid floating point value for frame rate ({1}).",
                    recorder.GetType().Name, frameRate);
                return false;
            }

            return true;
        }

        internal bool SessionCreated()
        {
            try
            {
                AllowInBackgroundMode();
                recordingStartTS = (Time.time / (Mathf.Approximately(Time.timeScale, 0f) ? 1f : Time.timeScale));
                sessionStartTS = DateTime.Now;
                recorder.SessionCreated(this);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        internal void RecreateRecorder()
        {
            if (settings != null && recorder == null)
            {
                recorder = RecordersInventory.CreateDefaultRecorder(settings);
            }
        }

        internal bool BeginRecording()
        {
            try
            {
                if (!settings.IsPlatformSupported)
                {
                    Debug.LogError(string.Format("Recorder {0} does not support current platform", recorder.GetType().Name));
                    return false;
                }

                AllowInBackgroundMode();

                if (!ValidateFrameRate(recorder.settings.FrameRate))
                    return false;

                recordingStartTS = (Time.time / (Mathf.Approximately(Time.timeScale, 0f) ? 1f : Time.timeScale));
                recorder.SignalInputsOfStage(ERecordingSessionStage.BeginRecording, this);

                // This must be after the above call otherwise GameView will not be initialized
                if (!recorder.BeginRecording(this))
                {
                    recorder.CleanupFailedRecording();
                    UnityHelpers.Destroy(recorderComponent); // so that the time scale is immediately reset (REC-834)
                    return false;
                }

                m_InitialFrame = Time.renderedFrameCount;
                m_FPSTimeStart = Time.unscaledTime;

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        internal void EndRecording()
        {
            try
            {
                recorder.SignalInputsOfStage(ERecordingSessionStage.EndRecording, this);
                recorder.EndRecording(this);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        internal void RecordFrame()
        {
            try
            {
                if (!recorder.SkipFrame(this))
                {
                    recorder.SignalInputsOfStage(ERecordingSessionStage.NewFrameReady, this);
                    recorder.RecordSubFrame(this);
                    if (!recorder.SkipSubFrame(this))
                    {
#if DEBUG_RECORDER_TIMING
                        Debug.LogFormat("Session::RecordFrame() index # {0} sub # {1} Out at frame # {2} - {3} - {4} ", m_FrameIndex, m_SubFrameIndex, Time.renderedFrameCount, Time.time, Time.deltaTime);
#endif
                        recorder.RecordFrame(this);
                        recorder.RecordedFramesCount++;
                        if (recorder.RecordedFramesCount == 1)
                            m_FirstRecordedFrameCount = Time.renderedFrameCount;
                    }
                    recorder.SignalInputsOfStage(ERecordingSessionStage.FrameDone, this);
                }
                else
                {
                    recorder.SignalInputsOfStage(ERecordingSessionStage.SkipFrame, this);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            using (capFPSMarker.Auto())
            {
                // Note: This is not great when multiple recorders are simultaneously active...
                if (settings.FrameRatePlayback == FrameRatePlayback.Variable ||
                    settings.FrameRatePlayback == FrameRatePlayback.Constant && recorder.settings.CapFrameRate)
                {
                    var frameCount = Time.renderedFrameCount - m_InitialFrame;
                    var frameLen = 1.0f / recorder.settings.FrameRate; // seconds
                    var elapsed = Time.unscaledTime - m_FPSTimeStart;
                    var target = frameLen * (frameCount + 1);
                    var sleepSec = Math.Min(target - elapsed, 1F);

                    if (sleepSec > 0.002F && !frameRateCapped)
                    {
                        frameRateCapped = true;
                        var curRealTime = Time.realtimeSinceStartup;
                        var endWaitTime = curRealTime + sleepSec;

                        if (RecorderOptions.VerboseMode)
                            Debug.Log(string.Format(
                                "Recording session info => dT: {0:F1}s, Target dT: {1:F1}s, Retarding: {2}s, fps: {3:F1}",
                                elapsed, target, sleepSec, frameCount / elapsed));

                        // We have a delta between the expected and actual current time. We'll sleep part of this delta, and
                        // use a busy loop for the rest. The idea is that typically, OS-level sleep calls are imprecise and
                        // may return "some time after the wanted duration".
                        var sleepMS = (int)(sleepSec * 1000) - 3;
                        if (sleepMS > 0)
                            System.Threading.Thread.Sleep(sleepMS);

                        // Do the rest of the wait using a busy loop to maximize chances of keeping the main thread running
                        // on the CPU.
                        while (Time.realtimeSinceStartup < endWaitTime) ;
                    }
                    else if (sleepSec < -frameLen)
                    {
                        m_InitialFrame--;
                    }
                    else if (RecorderOptions.VerboseMode)
                        Debug.Log(string.Format("Recording session info => fps: {0:F1}", frameCount / elapsed));

                    // reset every 30 frames
                    if (frameCount % 50 == 49)
                    {
                        m_FPSNextTimeStart = Time.unscaledTime;
                        m_FPSNextFrameCount = Time.renderedFrameCount;
                    }

                    if (frameCount % 100 == 99)
                    {
                        m_FPSTimeStart = m_FPSNextTimeStart;
                        m_InitialFrame = m_FPSNextFrameCount;
                    }
                }
            }

            if (!recorder.SkipSubFrame(this))
            {
                // This is a 'full' frame: increment the current full frame counter
                m_FrameIndex++;
            }
        }

        // Don't start incrementing the subframe count until accumulation has been activated.
        void IncrementSubFrameCount()
        {
#if HDRP_AVAILABLE
            if (settings.IsAccumulationSupported() && settings is IAccumulation accumulation && accumulation.GetAccumulationSettings().CaptureAccumulation)
            {
                switch (settings.RecordMode)
                {
                    case RecordMode.FrameInterval:
                    case RecordMode.SingleFrame:
                        if (m_FrameIndex >= settings.StartFrame)
                        {
                            m_SubFrameIndex++;
                        }
                        break;
                    case RecordMode.TimeInterval:
                        if (currentFrameStartTS >= settings.StartTime)
                        {
                            m_SubFrameIndex++;
                        }
                        break;
                    case RecordMode.Manual:
                        m_SubFrameIndex++;
                        break;
                    default:
                        throw new InvalidEnumArgumentException($"Unexpected record mode {settings.RecordMode}");
                }
            }
#endif
        }

        internal void PrepareNewFrame()
        {
            try
            {
                frameRateCapped = false;
                AllowInBackgroundMode();
                currentFrameStartTS =
                    (Time.time / (Mathf.Approximately(Time.timeScale, 0f) ? 1f : Time.timeScale)) -
                    recordingStartTS;
                IncrementSubFrameCount();
                if (!recorder.SkipFrame(this))
                {
                    recorder.SignalInputsOfStage(ERecordingSessionStage.NewFrameStarting, this);
                    recorder.PrepareNewFrame(this);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Cleans up the object's resources.
        /// </summary>
        public void Dispose()
        {
            if (recorder != null)
            {
                EndRecording();

                UnityHelpers.Destroy(recorder);
            }
        }
    }
}
