using System;

namespace UnityEditor.Recorder.Input
{
    /// <summary>
    /// This class contains the information for an AOV.
    /// </summary>
    public class AOVCameraInputSettings : CameraInputSettings
    {
        internal override bool SupportsFlipVertical => false;

        /// <summary>
        /// Input type for an AOV.
        /// </summary>
        protected internal override Type InputType
        {
#if HDRP_AVAILABLE
            get { return typeof(AOVCameraAOVRequestAPIInput);}
#else
            get { return typeof(AOVCameraDebugFrameworkInput); }
#endif
        }
    }
}
