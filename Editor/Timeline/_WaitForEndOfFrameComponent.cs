using System;
using UnityEngine;

namespace UnityEditor.Recorder.Timeline
{
    [ExecuteInEditMode]
    class WaitForEndOfFrameComponent : _FrameRequestComponent
    {
        [NonSerialized]
        public RecorderPlayableBehaviour m_playable;

        public void LateUpdate()
        {
            if (m_playable == null)
            {
                return;
            }

            if (m_playable.requestFrame)
            {
                RequestNewFrame();
            }
        }

        protected override void FrameReady()
        {
            if (m_playable != null)
                m_playable.FrameEnded();
        }
    }
}
