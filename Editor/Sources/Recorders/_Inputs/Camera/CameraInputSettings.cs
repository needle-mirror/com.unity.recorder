using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Recorder.Input
{
    /// <summary>
    /// Use this class to manage all the information required to record from a Scene Camera.
    /// </summary>
    [DisplayName("Targeted Camera")]
    [Serializable]
    public class CameraInputSettings : StandardImageInputSettings
    {
        internal const string k_UnsupportedCameraSourceError = "ActiveCamera is only compatible with the Legacy Render Pipeline.";
        internal const string k_UnsupportedRenderingPipelineError = "Targeted Camera capture is currently not compatible with the URP 2D Renderer. As an alternative, capture from the Game View or from a Render Texture Asset.";

        /// <summary>
        /// Indicates the Camera input type.
        /// </summary>
        public ImageSource Source
        {
            get { return source; }
            set { source = value; }
        }

        [SerializeField] private ImageSource source = ImageSource.MainCamera;

        /// <summary>
        /// Indicates the GameObject tag of the Camera used for the capture.
        /// </summary>
        public string CameraTag
        {
            get { return cameraTag; }
            set { cameraTag = value; }
        }

        [SerializeField] private string cameraTag;

        /// <summary>
        /// Use this property if you want to apply a vertical flip to the final output.
        /// </summary>
        public bool FlipFinalOutput
        {
            get { return flipFinalOutput; }
            set { flipFinalOutput = value; }
        }
        [SerializeField] private bool flipFinalOutput;

        /// <summary>
        /// Use this property to include the UI GameObjects to the recording.
        /// </summary>
        public bool CaptureUI
        {
            get { return captureUI; }
            set { captureUI = value; }
        }
        [SerializeField] private bool captureUI;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CameraInputSettings()
        {
            outputImageHeight = ImageHeight.Window;
        }

        /// <inheritdoc/>
        protected internal override Type InputType
        {
            get { return typeof(CameraInput); }
        }

        /// <inheritdoc/>
        protected internal override void CheckForErrors(List<string> errors)
        {
            base.CheckForErrors(errors);

            if (UnityHelpers.UsingURP() && UnityHelpers.UsingURP2DRenderer())
                errors.Add(k_UnsupportedRenderingPipelineError);

            if (Source == ImageSource.TaggedCamera)
            {
                if (string.IsNullOrEmpty(CameraTag))
                    errors.Add("Missing tag for camera selection");
                else
                {
                    try
                    {
                        var objs = GameObject.FindGameObjectsWithTag(CameraTag);
                        var cams = objs.Select(obj => obj.GetComponent<Camera>()).Where(c => c != null);

                        if (cams.Count() == 0)
                            errors.Add("No camera has the requested target tag '" + CameraTag + "'");
                    }
                    catch (UnityException)
                    {
                        errors.Add("The requested target tag '" + CameraTag + "' does not exist in the project");
                    }
                }
            }
            else if (Source == ImageSource.MainCamera && Camera.main == null)
            {
                errors.Add("There is no MainCamera in the project");
            }
            else if (Source == ImageSource.ActiveCamera && !UnityHelpers.UsingLegacyRP())
            {
                errors.Add(k_UnsupportedCameraSourceError);
            }
        }
    }
}
