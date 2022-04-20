using System;
using System.Collections;
using UnityEditor.Recorder.Timeline;
using UnityEngine;

namespace UnityEditor.Recorder
{
    // Hack to make new a new MonoBehaviour discovery feature in the engine.
    // This is because there is a mismatched between the MB name and the file name, all inside an Editor assembly (double nono).
    class _RecorderComponent {}

    class RecorderComponent : _FrameRequestComponent
    {
        public RecordingSession session { get; set; }
        public bool deferredStartRecording;
        public Func<bool> ShouldRequestFrameCb = () => true;
        public Action FrameReadyCb;

        public void Update()
        {
            // This is used by the timeline. Recording is started the next frame once the graphics system is initialized.
            // Otherwise there are issues with recorder clips starting at T=0 with play play on awake.
            if (deferredStartRecording)
            {
                StartCoroutine(DeferredStart());
                deferredStartRecording = false;
            }
        }

        IEnumerator DeferredStart()
        {
            yield return new WaitForEndOfFrame();
            StartRecording();
        }

        void StartRecording()
        {
            var fail = false;
            var wantsAccumulation = UnityHelpers.CaptureAccumulation(session.settings);

            //Accumulation can be enabled only when there are no recorders recording
            if (wantsAccumulation &&
                (RecorderPlayableBehaviour.recordingWithAccumulation ||
                 RecorderPlayableBehaviour.recordingWithoutAccumulation))
            {
                Debug.LogError(
                    $"{session.settings.name}: Cannot start this recording with accumulation because another recording is already running.");
                fail = true;
            }

            if (!fail && RecorderPlayableBehaviour.recordingWithAccumulation)
            {
                Debug.LogError(
                    "Cannot start the recording session because a pre-existing session with accumulation is present.");
                fail = true;
            }

            if (!fail)
            {
                var res = session.BeginRecording();
                fail = !res;
#if UNITY_EDITOR
                RecorderAnalytics.SendStartEvent(session);
                if (!res)
                {
                    RecorderAnalytics.SendStopEvent(session, true, false);
                }
                else
                {
                    if (wantsAccumulation)
                    {
                        RecorderPlayableBehaviour.recordingWithAccumulation = true;
                    }
                    else
                    {
                        RecorderPlayableBehaviour.recordingWithoutAccumulation = true;
                    }
                }
            }
#endif
            if (fail)
            {
                DestroyImmediate(this);
            }
        }

        public void LateUpdate()
        {
            if (!ShouldRequestFrameCb())
            {
                return;
            }

            if (session != null && session.isRecording)
            {
                session.PrepareNewFrame();
            }

            if (session != null && session.isRecording)
            {
                RequestNewFrame();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (session != null)
                session.Dispose();
        }

        protected override void FrameReady()
        {
            if (FrameReadyCb != null)
            {
                FrameReadyCb();
            }
#if DEBUG_RECORDER_TIMING
            Debug.LogFormat("FrameReady Out at frame # {0} - {1} - {2} ", Time.renderedFrameCount, Time.time, Time.deltaTime);
#endif
#if DEBUG_RECORDER_TIMING
            Debug.LogFormat("FrameReady IN at frame # {0} - {1} - {2} ", Time.renderedFrameCount, Time.time, Time.deltaTime);
#endif
            session.RecordFrame();

            switch (session.recorder.settings.RecordMode)
            {
                case RecordMode.Manual:
                    break;
                case RecordMode.SingleFrame:
                {
                    // We are done recording once the frame has been recorded.
                    if (session.recorder.RecordedFramesCount == 1)
                        Destroy(this);
                    break;
                }
                case RecordMode.FrameInterval:
                {
                    // We are done recording once the expected number of frames has been recorded.
                    var expectedFrames = (session.settings.EndFrame - session.settings.StartFrame);
                    if (session.settings.FrameRatePlayback == FrameRatePlayback.Variable)
                    {
                        expectedFrames /= session.settings.captureEveryNthFrame;
                    }
                    if (session.recorder.RecordedFramesCount > expectedFrames)
                    {
                        Destroy(this);
                    }
                    break;
                }
                case RecordMode.TimeInterval:
                {
                    var expectedFrames = (session.settings.EndTime - session.settings.StartTime) *
                                         session.settings.FrameRate;
                    if (session.settings.FrameRatePlayback == FrameRatePlayback.Variable)
                    {
                        expectedFrames /= session.settings.captureEveryNthFrame;
                        if (session.recorder.RecordedFramesCount > expectedFrames)
                        {
                            Destroy(this);
                        }
                    }
                    else
                    {
                        // We are done recording once the expected number of frames has been recorded.
                        if (session.recorder.RecordedFramesCount > expectedFrames)
                        {
                            Destroy(this);
                        }
                    }

                    break;
                }
            }
        }
    }
}
