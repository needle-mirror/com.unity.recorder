using UnityEngine;
using UnityEngine.Playables;

namespace UnityEditor.Recorder.Timeline
{
    class RecorderPlayableBehaviour : PlayableBehaviour
    {
        PlayState m_PlayState = PlayState.Paused;
        public RecordingSession session { get; set; }
        public static bool recordingWithAccumulation { get; set; }
        public static bool recordingWithoutAccumulation { get; set; }
        RecorderComponent endOfFrameComp;
        bool m_FirstOneSkipped;

        bool m_RequestFrame = true;

        public override void OnGraphStart(Playable playable)
        {
            if (session != null)
            {
                // does not support multiple starts...
                session.SessionCreated();
                m_PlayState = PlayState.Paused;
            }
        }

        public override void OnGraphStop(Playable playable)
        {
            if (session != null && session.isRecording)
            {
                session.EndRecording();
                session.Dispose();
                session = null;
            }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            m_RequestFrame = true;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (session == null)
                return;
            m_PlayState = PlayState.Playing;

            if (session != null)
            {
                if (endOfFrameComp == null)
                {
                    endOfFrameComp = session.recorderGameObject.AddComponent<RecorderComponent>();
                    endOfFrameComp.session = session;
                    endOfFrameComp.deferredStartRecording = true;
                    endOfFrameComp.ShouldRequestFrameCb = () => m_RequestFrame;
                    endOfFrameComp.FrameReadyCb = FrameEnded;
                    session.recorderComponent = endOfFrameComp;
                }
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (session == null)
                return;

            if (session.isRecording && m_PlayState == PlayState.Playing)
            {
#if UNITY_EDITOR
                const double eps = 1e-5; // end is never evaluated
                RecorderAnalytics.SendStopEvent(session, false, playable.GetTime() >= playable.GetDuration() - eps);
#endif
                recordingWithAccumulation = false;
                recordingWithoutAccumulation = false;
                session.Dispose();
                session = null;
                Object.DestroyImmediate(endOfFrameComp);
                endOfFrameComp = null;
            }

            m_PlayState = PlayState.Paused;
        }

        void FrameEnded()
        {
            if (session != null && session.isRecording)
            {
                m_RequestFrame = false;
            }
        }
    }
}
