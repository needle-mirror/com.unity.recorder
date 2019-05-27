using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace UnityEditor.Recorder.Input
{
    [DisplayName("Targeted Camera")]
    [Serializable]
    public class CameraInputSettings : StandardImageInputSettings
    {
        public ImageSource source = ImageSource.ActiveCamera;
        public string cameraTag;
        public bool flipFinalOutput;
        public bool captureUI;

        internal static bool IsHDRPAvailable()
        {
            // For backward compatibility with unity version < 19.1
            // Use reflection to determine if hdrp is available 
            const string ClassName = "UnityEngine.Experimental.Rendering.HDPipeline.HDRenderPipeline";
            const string editorDllName = "Unity.RenderPipelines.HighDefinition.Runtime";
            var hdrpRenderPipeline = Type.GetType(ClassName + ", " + editorDllName );
            return (hdrpRenderPipeline != null);
        }
        
        public CameraInputSettings()
        {
            if (IsHDRPAvailable())
            {
                source = ImageSource.MainCamera;
            }
            outputImageHeight = ImageHeight.Window;
        }
        
        internal override Type inputType
        {
            get { return typeof(CameraInput); }
        }

        internal override bool ValidityCheck(List<string> errors)
        {
            var ok = base.ValidityCheck(errors);
            if (source == ImageSource.TaggedCamera && string.IsNullOrEmpty(cameraTag))
            {
                ok = false;
                errors.Add("Missing tag for camera selection");
            }

            return ok;
        }
    }
}
