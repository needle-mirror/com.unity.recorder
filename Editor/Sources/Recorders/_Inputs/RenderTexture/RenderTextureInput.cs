using System;
using System.ComponentModel;
using UnityEngine;

namespace UnityEditor.Recorder.Input
{
    class RenderTextureInput : BaseRenderTextureInput
    {
        // Whether or not the incoming RenderTexture must be flipped vertically
        private bool m_needToFlipVertically = false;

        // Whether or not the incoming RenderTexture must be converted from linear to sRGB color space
        private bool m_needToConvertLinearToSRGB = false;

        RenderTextureInputSettings cbSettings => (RenderTextureInputSettings)settings;

        // An internal RenderTexture copied from the input RenderTexture when vertical flip and/or sRGB conversion need to be performed
        internal RenderTexture workTexture = null;

        // A material to perform vertical flips and/or sRGB conversion of a RenderTexture
        private Material MaterialRenderTextureCopy
        {
            get
            {
                if (_matSRGBConversion == null)
                    _matSRGBConversion = new Material(Shader.Find("Hidden/RenderTextureCopy"));
                return _matSRGBConversion;
            }
        }
        private Material _matSRGBConversion = null; // a shader for doing linear to sRGB conversion

        protected internal override void BeginRecording(RecordingSession session)
        {
            if (cbSettings.renderTexture == null)
                return; // error will have been triggered in RenderTextureInputSettings.CheckForErrors()

            OutputHeight = cbSettings.OutputHeight;
            OutputWidth = cbSettings.OutputWidth;
            OutputRenderTexture = cbSettings.renderTexture;

            m_needToFlipVertically = cbSettings.FlipFinalOutput; // whether or not the recorder settings have the flip box checked
            var movieRecorderSettings = session.settings as MovieRecorderSettings;
            if (movieRecorderSettings != null)
            {
                bool encoderAlreadyFlips = movieRecorderSettings.encodersRegistered[movieRecorderSettings.encoderSelected].PerformsVerticalFlip;
                m_needToFlipVertically = m_needToFlipVertically ? encoderAlreadyFlips : !encoderAlreadyFlips;
            }

            m_needToConvertLinearToSRGB = session.settings.NeedToConvertFromLinearToSRGB();
            if (m_needToFlipVertically || m_needToConvertLinearToSRGB)
            {
                workTexture = new RenderTexture(OutputRenderTexture);
                workTexture.name = "RenderTextureInput_intermediate";
            }
        }

        protected internal override void NewFrameReady(RecordingSession session)
        {
            if (cbSettings.renderTexture == null)
                return; // error will have been triggered in BeginRecording()

            if (m_needToFlipVertically)
                MaterialRenderTextureCopy.EnableKeyword("VERTICAL_FLIP");

            if (m_needToConvertLinearToSRGB)
                MaterialRenderTextureCopy.EnableKeyword("SRGB_CONVERSION");

            if (m_needToFlipVertically || m_needToConvertLinearToSRGB)
            {
                // Perform the actual conversion
                var rememberActive = RenderTexture.active;
                Graphics.Blit(cbSettings.renderTexture, workTexture, MaterialRenderTextureCopy);
                RenderTexture.active = rememberActive; // restore active RT

                // Make the final texture use the modified texture
                OutputRenderTexture = workTexture;
            }
            else
            {
                // Just use the input without any changes
                OutputRenderTexture = cbSettings.renderTexture;
            }

            base.NewFrameReady(session);
        }

        protected internal override void EndRecording(RecordingSession session)
        {
            base.EndRecording(session);

            if (workTexture != null)
            {
                UnityHelpers.Destroy(workTexture);
                workTexture = null;
            }
        }
    }
}
