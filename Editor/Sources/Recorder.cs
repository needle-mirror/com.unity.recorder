using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace UnityEditor.Recorder
{
    internal enum ERecordingSessionStage
    {
        BeginRecording,
        NewFrameStarting,
        NewFrameReady,
        SkipFrame,
        FrameDone,
        EndRecording,
        SessionCreated
    }

    /// <summary>
    /// Base class for all Recorders. To create a new Recorder, extend <see cref="GenericRecorder{T}"/>.
    /// </summary>
    public abstract class Recorder : ScriptableObject
    {
        static int sm_CaptureFrameRateCount;
        bool m_ModifiedCaptureFR;
        double m_FrameInterval;
        bool m_TimePadDisabled;
        float m_SessionStartTime;
        double m_FramePadTime = 0; // Time padding seconds to fix JIRA REC-1105

        private static bool s_asyncShaderCompileSetting;
        private static bool s_asyncShaderCompileAlreadyRestored = false; // have we already restored the value of the setting?
        private static bool s_asyncShaderCompileAlreadyDisabled = false; // have we already disabled the setting?

        /// <summary>
        /// Indicates the number of frames of the current recording session.
        /// </summary>
        protected internal int RecordedFramesCount { get; internal set; }

        /// <summary>
        /// The list of inputs to the Recorder, representing the sources of the captured data.
        /// </summary>
        protected List<RecorderInput> m_Inputs;

        void Awake()
        {
            sm_CaptureFrameRateCount = 0;
        }

        protected internal virtual void Reset()
        {
            RecordedFramesCount = 0;
            Recording = false;
        }

        void OnDestroy()
        {
            if (m_ModifiedCaptureFR)
            {
                sm_CaptureFrameRateCount--;
                if (sm_CaptureFrameRateCount == 0)
                {
                    Time.captureFramerate = 0;
                    if (RecorderOptions.VerboseMode)
                        Debug.Log("Recorder resetting 'CaptureFrameRate' to zero");
                }
            }
        }

        internal abstract RecorderSettings settings { get; set; }

        protected internal void ConsoleLogMessage(string message, LogType logType)
        {
            string sAborted = "";
            switch (logType)
            {
                case LogType.Warning:
                    sAborted = "Recording may cause slowdowns or generate an invalid file. ";
                    break;
                case LogType.Error:
                    sAborted = "Recording failed. ";
                    break;
                default:
                    break;
            }
            string logMessage = $"[{GetType().Name}: {settings.name}] {sAborted}{message}";
            switch (logType)
            {
                case LogType.Log:
                    Debug.Log(logMessage);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(logMessage);
                    break;
                case LogType.Error:
                    Debug.LogError(logMessage);
                    break;
                default:
                    throw new InvalidEnumArgumentException($"Log type {logType} is not supported yet.");
            }
        }

        protected internal virtual void SessionCreated(RecordingSession session)
        {
            if (RecorderOptions.VerboseMode)
                ConsoleLogMessage("Session created", LogType.Log);

            var fixedRate = settings.FrameRatePlayback == FrameRatePlayback.Constant ? settings.FrameRate : 0.0f;
            if (fixedRate > 0)
            {
                var toComparePeriodSeconds = 1.0f / fixedRate; // Hz -> secs

#if RECORDER_DISABLE_FRAME_TIME_PADDING
                m_FramePadTime = 0;
#else
                m_FramePadTime = 1e-7; // Need to add some time to the Time.captureDeltaTime to prevent double frame capture (REC-1105)
#endif

                if (Time.captureFramerate != 0 && Math.Abs(toComparePeriodSeconds - Time.captureDeltaTime) > (float.Epsilon + m_FramePadTime))
                    ConsoleLogMessage($"Recorder {GetType().Name} is set to record at a fixed rate and another component has already set a conflicting value for [Time.captureFramerate], new value being applied : {fixedRate}!", LogType.Error);
                else if (Time.captureFramerate == 0 && RecorderOptions.VerboseMode)
                    ConsoleLogMessage($"Frame recorder set fixed frame rate to {fixedRate}", LogType.Log);
                // Note that Time.captureDeltaTime will be modified by HDRP SubFrameManager
                // to implement the accumulation motion blur/path tracer support.

                m_SessionStartTime = Time.time;
                m_FrameInterval = (1.0d / (double)fixedRate); // Store the expected frame interval

#if RECORDER_DISABLE_FRAME_TIME_PADDING
                Time.captureDeltaTime = (float)(m_FrameInterval); // Do not use the "time padding" workaround (REC-1105)
#else
                Time.captureDeltaTime = (float)(m_FrameInterval + m_FramePadTime); // Need to add some time to the Time.captureDeltaTime to prevent double frame capture (REC-1105)
#endif

                sm_CaptureFrameRateCount++;
                m_ModifiedCaptureFR = true;
            }

            m_Inputs = new List<RecorderInput>();
            foreach (var inputSettings in settings.InputsSettings)
            {
                var input = (RecorderInput)Activator.CreateInstance(inputSettings.InputType);
                input.settings = inputSettings;
                m_Inputs.Add(input);
                SignalInputsOfStage(ERecordingSessionStage.SessionCreated, session);
            }
        }

        /// <summary>
        /// Starts a new recording session. Callback is invoked once when the recording session starts.
        /// </summary>
        /// <param name="session">The newly created recording session.</param>
        /// <returns>True if recording can start, False otherwise.</returns>
        /// <exception cref="Exception">Throws if there is already a recording session running.</exception>
        protected internal virtual bool BeginRecording(RecordingSession session)
        {
            if (Recording)
                throw new Exception("Already recording!");

            // Log old warnings (non-blocking)
            var oldWarnings = new List<string>();
#pragma warning disable 618
            if (!session.settings.ValidityCheck(oldWarnings))
#pragma warning restore 618
            {
                foreach (var w in oldWarnings)
                    ConsoleLogMessage(w, LogType.Warning);
            }

            // Log non-blocking warnings
            var warnings = new List<string>();
            session.settings.GetWarnings(warnings);
            foreach (var w in warnings)
                ConsoleLogMessage(w, LogType.Warning);

            // Log blocking errors and stop
            var errors = new List<string>();
            session.settings.GetErrors(errors);
            foreach (var w in errors)
                ConsoleLogMessage(w, LogType.Error);
            if (errors.Count > 0)
            {
                Recording = false;
                return false;
            }

            if (RecorderOptions.VerboseMode)
                ConsoleLogMessage($"Starting to record", LogType.Log);

            DisableAsyncShaderCompil();

            return Recording = true;
        }

        /// <summary>
        /// Ends the current recording session. Callback is invoked when the recording session ends.
        /// </summary>
        /// <param name="session">The current recording session.</param>
        protected internal virtual void EndRecording(RecordingSession session)
        {
            if (!Recording)
                return;

            Recording = false;

            if (m_ModifiedCaptureFR)
            {
                m_ModifiedCaptureFR = false;
                sm_CaptureFrameRateCount--;
                if (sm_CaptureFrameRateCount == 0)
                {
                    Time.captureFramerate = 0;
                    if (RecorderOptions.VerboseMode)
                        ConsoleLogMessage("Recorder resetting 'CaptureFrameRate' to zero", LogType.Log);
                }
            }

            foreach (var input in m_Inputs)
            {
                if (input != null)
                    input.Dispose();
            }

            if (RecorderOptions.VerboseMode)
                ConsoleLogMessage($"Recording stopped, total frame count: {RecordedFramesCount}", LogType.Log);

            RestoreAsynchronousShaderCompilation();
            ++settings.Take;

            // When adding a file to Unity's assets directory, trigger a refresh so it is detected.
            if (settings.fileNameGenerator.Root == OutputPath.Root.AssetsFolder || settings.fileNameGenerator.Root == OutputPath.Root.StreamingAssets)
                AssetDatabase.Refresh();
        }

        /// <summary>
        /// Records a single frame. Callback is invoked for every frame during the recording session.
        /// </summary>
        /// <param name="ctx">The current recording session.</param>
        protected internal abstract void RecordFrame(RecordingSession ctx);

        /// <summary>
        /// Prepares a frame before recording it. Callback is invoked for every frame during the recording session, before RecordFrame.
        /// </summary>
        /// <param name="ctx">The current recording session.</param>
        protected internal virtual void PrepareNewFrame(RecordingSession ctx)
        {
#if !RECORDER_DISABLE_FRAME_TIME_PADDING
            ResetDeltaTime();
#endif
        }

        /// <summary>
        /// To prevent capturing frames too early (resulting in double frames captured),  due to
        /// Time.captureDeltaTime rounding errors, Time.captureDeltaTime must be padded a bit
        /// to ensure the sample is taken on the right side of the frame time boundary. This only
        /// needs to be done on the first 2 frames it seems, after that reset Time.captureDeltaTime
        /// back to the frame time interval. This addresses Jira issue REC-1105.
        /// Use define symbol RECORDER_DISABLE_FRAME_TIME_PADDING to disable this.
        /// </summary>
        internal void ResetDeltaTime()
        {
            if (m_TimePadDisabled && Time.captureDeltaTime >= (float)m_FrameInterval)
            {
                // Time Padding is disabled
                Time.captureDeltaTime = (float)m_FrameInterval; // Set captureDeltaTime back to the frame interval without any padding since this workaround is only needed for the first few frames
                m_FramePadTime = 0;
            }
            else
            {
                // Time Padding is enabled
                m_TimePadDisabled = (Time.time - m_SessionStartTime > 0f) ? true : false; // Disable padding this after we get past time 0
            }
        }

        internal virtual void RecordSubFrame(RecordingSession ctx)
        {
        }

        /// <summary>
        /// Tests if a frame should be skipped before trying to record it. Callback is invoked for every frame during the recording session.
        /// </summary>
        /// <remarks>
        /// If this function returns True, RecordFrame will not be invoked.
        /// </remarks>
        /// <param name="ctx">The current recording session.</param>
        /// <returns>True if the frame should be skipped, False otherwise.</returns>
        protected internal virtual bool SkipFrame(RecordingSession ctx)
        {
            // Compute the starting frame of the recording
            int startFrame = settings.RecordMode == RecordMode.TimeInterval ? (int)(settings.StartTime * settings.FrameRate) : settings.StartFrame;
            var skip_vfr = ctx.frameIndex % settings.captureEveryNthFrame != 0 &&
                ctx.settings.FrameRatePlayback == FrameRatePlayback.Variable;
            var skip_time = settings.RecordMode == RecordMode.TimeInterval && ctx.frameIndex < startFrame;
            var skip_frame = settings.RecordMode == RecordMode.FrameInterval && ctx.frameIndex < startFrame;
            var skip_single = settings.RecordMode == RecordMode.SingleFrame && ctx.frameIndex < startFrame;
            var result = !Recording
                || skip_vfr
                || skip_time
                || skip_frame
                || skip_single;
            return result;
        }

        /// <summary>
        /// Tests if a sub frame should be skipped before trying to record it. Callback is invoked for every frame during the recording session.
        /// </summary>
        /// <remarks>
        /// If this function returns True, RecordFrame will not be invoked.
        /// </remarks>
        /// <param name="ctx">The current recording session.</param>
        /// <returns>True if the sub frame should be skipped, False otherwise.</returns>
        protected internal virtual bool SkipSubFrame(RecordingSession ctx)
        {
            if (!settings.IsAccumulationSupported())
                return false;
            IAccumulation accumulation = settings as IAccumulation;

            AccumulationSettings accumulationSettings = null;

            if (accumulation != null)
            {
                accumulationSettings = accumulation.GetAccumulationSettings();
            }

            int accumulationSamples = 1;
            if (accumulationSettings != null &&  accumulationSettings.CaptureAccumulation)
            {
                accumulationSamples = accumulationSettings.Samples;
            }

            bool skip = (ctx.subFrameIndex) % accumulationSamples != 0;
            return skip;
        }

        /// <summary>
        /// Tests if there is a recording session currently running.
        /// </summary>
        /// <returns>True if a recording session is currently active, False otherwise.</returns>
        public bool Recording { get; protected set; }

        internal void SignalInputsOfStage(ERecordingSessionStage stage, RecordingSession session)
        {
            if (m_Inputs == null)
                return;

            switch (stage)
            {
                case ERecordingSessionStage.SessionCreated:
                    foreach (var input in m_Inputs)
                        input.SessionCreated(session);
                    break;
                case ERecordingSessionStage.BeginRecording:
                    foreach (var input in m_Inputs)
                        input.BeginRecording(session);
                    break;
                case ERecordingSessionStage.NewFrameStarting:
                    foreach (var input in m_Inputs)
                        input.NewFrameStarting(session);
                    break;
                case ERecordingSessionStage.NewFrameReady:
                    foreach (var input in m_Inputs)
                        input.NewFrameReady(session);
                    break;
                case ERecordingSessionStage.SkipFrame:
                    foreach (var input in m_Inputs)
                        input.SkipFrame(session);
                    break;

                case ERecordingSessionStage.FrameDone:
                    foreach (var input in m_Inputs)
                        input.FrameDone(session);
                    break;
                case ERecordingSessionStage.EndRecording:
                    foreach (var input in m_Inputs)
                        input.EndRecording(session);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("stage", stage, null);
            }
        }

        /// <summary>
        /// Disable asynchronous shader compilation and save the status.
        /// </summary>
        private static void DisableAsyncShaderCompil()
        {
            if (s_asyncShaderCompileAlreadyDisabled)
                return;

            // Save the async compile shader setting to restore it at the end of recording
            s_asyncShaderCompileSetting = EditorSettings.asyncShaderCompilation;
            // Disable async compile shader setting when recording
            EditorSettings.asyncShaderCompilation = false;
            s_asyncShaderCompileAlreadyRestored = false;
            s_asyncShaderCompileAlreadyDisabled = true;
        }

        /// <summary>
        /// If we have not already restored the setting of asynchronous shader compilation, restore it.
        /// </summary>
        private static void RestoreAsynchronousShaderCompilation()
        {
            if (s_asyncShaderCompileAlreadyRestored || !s_asyncShaderCompileAlreadyDisabled)
                return;
            EditorSettings.asyncShaderCompilation = s_asyncShaderCompileSetting;
            s_asyncShaderCompileAlreadyRestored = true;
            s_asyncShaderCompileAlreadyDisabled = false;
        }

        /// <summary>
        /// If a recording fails, clean up after it.
        /// </summary>
        internal void CleanupFailedRecording()
        {
            RestoreAsynchronousShaderCompilation();
        }

        public void Pause()
        {
            Recording = false;
        }

        public void Resume()
        {
            Recording = true;
        }
    }
}
