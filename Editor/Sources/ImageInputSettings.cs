using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Recorder.Input
{
    /// <inheritdoc />
    /// <summary>
    /// Optional base class for image related inputs.
    /// </summary>
    public abstract class ImageInputSettings : RecorderInputSettings
    {
        /// <summary>
        /// Stores the output image width.
        /// </summary>
        public abstract int OutputWidth { get; set; }
        /// <summary>
        /// Stores the output image height.
        /// </summary>
        public abstract int OutputHeight { get; set; }

        /// <summary>
        /// Indicates if derived classes support transparency.
        /// </summary>
        public virtual bool SupportsTransparent
        {
            get { return true; }
        }

        /// <summary>
        /// This property indicates that the alpha channel should be grabbed from the GPU.
        /// </summary>
        public bool RecordTransparency { get; set; }
    }

    /// <inheritdoc />
    /// <summary>
    /// This class regroups settings required to specify the size of an image input using a size and an aspect ratio.
    /// </summary>
    [Serializable]
    public abstract class StandardImageInputSettings : ImageInputSettings
    {
        /// <summary>
        /// Goes with 4 parameters
        /// 0-1 = GameView's resolution
        /// 2-3 = Requested resolution
        /// </summary>
        internal const string k_GameViewResolutionMismatchErrorFormat =
            "The Game view resolution ({0}x{1}) does not match the requested resolution ({2}x{3}). "
            + "Recorder needs to force the Game View resolution to the requested one for the recording.\n"
            + "After the recording, use the Game View control bar to restore the resolution to your preferred value.";

        [SerializeField]
        OutputResolution m_OutputResolution = new OutputResolution();

        /// <inheritdoc />
        public override int OutputWidth
        {
            get => m_OutputResolution.GetWidth();
            set => m_OutputResolution.SetWidth(value);
        }

        /// <inheritdoc />
        public override int OutputHeight
        {
            get => m_OutputResolution.GetHeight();
            set => m_OutputResolution.SetHeight(value);
        }

        internal ImageHeight outputImageHeight
        {
            get { return m_OutputResolution.imageHeight; }
            set { m_OutputResolution.imageHeight = value; }
        }

        internal ImageHeight maxSupportedSize
        {
            get { return m_OutputResolution.maxSupportedHeight; }
            set { m_OutputResolution.maxSupportedHeight = value; }
        }

        protected internal override void CheckForWarnings(List<string> warnings)
        {
            base.CheckForWarnings(warnings);

            if (OutputHeight > (int)maxSupportedSize)
                warnings.Add($"The image size exceeds the recommended maximum height of {(int)maxSupportedSize} px: {OutputHeight}");

            GameViewSize.GetGameRenderSize(out var w, out var h);
            if (w != m_OutputResolution.GetWidth() || h != m_OutputResolution.GetHeight())
                warnings.Add(String.Format(k_GameViewResolutionMismatchErrorFormat, w, h, m_OutputResolution.GetWidth(), m_OutputResolution.GetHeight()));
        }

        protected internal override void CheckForErrors(List<string> errors)
        {
            base.CheckForErrors(errors);

            var h = OutputHeight;
            var w = OutputWidth;

            if (w <= 0 || h <= 0)
                errors.Add($"Invalid source image resolution {w}x{h}");
        }
    }
}
