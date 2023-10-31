using System;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace UnityEditor.Recorder
{
    [Serializable]
    class AOVImageInputSelector : InputSettingsSelector
    {
        [SerializeField] public CameraInputSettings cameraInputSettings = new AOVCameraInputSettings();
        public ImageInputSettings imageInputSettings
        {
            get { return (ImageInputSettings)Selected; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (value is CameraInputSettings)
                {
                    Selected = value;
                }
                else
                {
                    throw new ArgumentException("Video input type not supported: '" + value.GetType() + "'");
                }
            }
        }
    }
}
