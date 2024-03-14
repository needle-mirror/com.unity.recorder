using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace UnityEditor.Recorder
{
    /// <summary>
    /// A class that represents the settings of an Image Recorder.
    /// </summary>
    [RecorderSettings(typeof(ImageRecorder), "Image Sequence", "imagesequence_16")]
    public class ImageRecorderSettings : RecorderSettings, IAccumulation, RecorderSettings.IResolutionUser
    {
        /// <summary>
        /// Available options for the output image format used by Image Sequence Recorder.
        /// </summary>
        public enum ImageRecorderOutputFormat
        {
            /// <summary>
            /// Output the recording in PNG format.
            /// </summary>
            PNG,
            /// <summary>
            /// Output the recording in JPEG format.
            /// </summary>
            JPEG,
            /// <summary>
            /// Output the recording in EXR format.
            /// </summary>
            EXR
        }

        /// <summary>
        /// Compression type for EXR files.
        /// </summary>
        [Obsolete("Use CompressionUtility.EXRCompressionType instead. (UnityUpgradable) -> UnityEditor.Recorder.CompressionUtility/EXRCompressionType")]
        public enum EXRCompressionType
        {
            /// <summary>
            /// No compression.
            /// </summary>
            None,
            /// <summary>
            /// Run-length encoding compression.
            /// </summary>
            RLE,
            /// <summary>
            /// Zip compression.
            /// </summary>
            Zip,
            /// <summary>
            /// Wavelet compression.
            /// </summary>
            PIZ,
        }

        internal static bool IsAvailableForImageSequence(CompressionUtility.EXRCompressionType compressionType)
        {
            switch (compressionType)
            {
                case CompressionUtility.EXRCompressionType.None:
                case CompressionUtility.EXRCompressionType.RLE:
                case CompressionUtility.EXRCompressionType.Zip:
                case CompressionUtility.EXRCompressionType.PIZ:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Color Space (gamma curve, gamut) to use in the output images.
        /// </summary>
        public enum ColorSpaceType
        {
            /// <summary>
            /// The sRGB color space.
            /// </summary>
            sRGB_sRGB,
            /// <summary>
            /// The linear sRGB color space.
            /// </summary>
            Unclamped_linear_sRGB
        }

        /// <summary>
        /// Stores the output image format currently used for this Recorder.
        /// </summary>
        public ImageRecorderOutputFormat OutputFormat
        {
            get { return outputFormat; }
            set { outputFormat = value; }
        }

        [SerializeField] ImageRecorderOutputFormat outputFormat = ImageRecorderOutputFormat.JPEG;

        /// <summary>
        /// Use this property to capture the alpha channel (True) or not (False) in the output.
        /// </summary>
        /// <remarks>
        /// Alpha channel is captured only if the output image format supports it.
        /// </remarks>
        public bool CaptureAlpha
        {
            get { return captureAlpha; }
            set
            {
                captureAlpha = value;
                imageInputSettings.RecordTransparency = CaptureAlpha;
            }
        }

        [SerializeField] private bool captureAlpha;

        /// <summary>
        /// The JPEG encoding quality level. Range is 1 to 100. Default value is 75.
        /// </summary>
        public int JpegQuality
        {
            get { return m_JpegQuality; }
            set { m_JpegQuality = value; }
        }

        [SerializeField] private int m_JpegQuality = 75;

        /// <summary>
        /// Use this property to capture the frames in HDR (if the setup supports it).
        /// </summary>
        public bool CaptureHDR
        {
            get { return CanCaptureHDRFrames() && m_ColorSpace == ColorSpaceType.Unclamped_linear_sRGB; }
        }


        [SerializeField] ImageInputSelector m_ImageInputSelector = new ImageInputSelector();
        [SerializeField] internal CompressionUtility.EXRCompressionType m_EXRCompression = CompressionUtility.EXRCompressionType.Zip;
        [SerializeField] internal ColorSpaceType m_ColorSpace = ColorSpaceType.Unclamped_linear_sRGB;
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ImageRecorderSettings()
        {
            fileNameGenerator.FileName = DefaultWildcard.Recorder + "_" + DefaultWildcard.Take + "_" + DefaultWildcard.Frame;
        }

        /// <inheritdoc/>
        protected internal override string Extension
        {
            get
            {
                switch (OutputFormat)
                {
                    case ImageRecorderOutputFormat.PNG:
                        return "png";
                    case ImageRecorderOutputFormat.JPEG:
                        return "jpg";
                    case ImageRecorderOutputFormat.EXR:
                        return "exr";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Stores the data compression method to use to encode image files in the EXR format.
        /// </summary>
        public CompressionUtility.EXRCompressionType EXRCompression
        {
            get => m_EXRCompression;
            set => m_EXRCompression = value;
        }

        /// <summary>
        /// Stores the color space to use to encode the output image files.
        /// </summary>
        public ColorSpaceType OutputColorSpace
        {
            get => m_ColorSpace;
            set => m_ColorSpace = value;
        }

        // This is necessary because the value in OutputColorSpace might hold invalid information (e.g. for PNG and JPEG it
        // could say Linear) because the value doesn't change when the output format is changed.
        // See the handling of the color space dropdown in ImageRecorderEditor.FileTypeAndFormatGUI.
        internal ColorSpaceType OutputColorSpaceComputed
        {
            get
            {
                switch (OutputFormat)
                {
                    case ImageRecorderOutputFormat.PNG:
                    case ImageRecorderOutputFormat.JPEG:
                        return ColorSpaceType.sRGB_sRGB; // these formats must always be sRGB
                    case ImageRecorderOutputFormat.EXR:
                        if (CanCaptureHDRFrames())
                            return OutputColorSpace;
                        else
                            return ColorSpaceType.sRGB_sRGB; // must be sRGB
                    default:
                        throw new InvalidEnumArgumentException($"Unexpected output format {OutputFormat}");
                }
            }
        }

        /// <summary>
        /// The settings of the input image.
        /// </summary>
        public ImageInputSettings imageInputSettings
        {
            get { return m_ImageInputSelector.ImageInputSettings; }
            set { m_ImageInputSelector.ImageInputSettings = value; }
        }

        /// <summary>
        /// The list of settings of the Recorder Inputs.
        /// </summary>
        public override IEnumerable<RecorderInputSettings> InputsSettings
        {
            get { yield return m_ImageInputSelector.Selected; }
        }

        internal bool CanCaptureHDRFrames()
        {
            bool isGameViewInput = imageInputSettings.InputType == typeof(GameViewInput);
            bool isFormatExr = OutputFormat == ImageRecorderOutputFormat.EXR;
            return !isGameViewInput && isFormatExr && UnityHelpers.UsingHDRP();
        }

        internal bool CanCaptureAlpha()
        {
            bool formatSupportAlpha = OutputFormat == ImageRecorderOutputFormat.PNG ||
                OutputFormat == ImageRecorderOutputFormat.EXR;
            bool inputSupportAlpha = imageInputSettings.SupportsTransparent;
            return (formatSupportAlpha && inputSupportAlpha && !UnityHelpers.UsingURP());
        }

        [SerializeReference] AccumulationSettings _accumulationSettings = new AccumulationSettings();

        /// <summary>
        /// Stores the AccumulationSettings properties
        /// </summary>
        public AccumulationSettings AccumulationSettings
        {
            get { return _accumulationSettings; }
            set { _accumulationSettings = value; }
        }

        /// <summary>
        /// Use this method to get all the AccumulationSettings properties.
        /// </summary>
        /// <returns>AccumulationSettings</returns>
        public AccumulationSettings GetAccumulationSettings()
        {
            return AccumulationSettings;
        }

        /// <inheritdoc/>
        public override bool IsAccumulationSupported()
        {
            if (GetAccumulationSettings() != null)
            {
                var cis = m_ImageInputSelector.Selected as CameraInputSettings;
                var gis = m_ImageInputSelector.Selected as GameViewInputSettings;
                if (cis != null || gis != null)
                {
                    return true;
                }
            }
            return false;
        }

        internal override void OnValidate()
        {
            base.OnValidate();
            imageInputSettings.RecordTransparency = CaptureAlpha; // We need to sync the input data, when the UI changes the recorder one
        }

        internal override bool HasWarnings()
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            ValidateRecording(errors, warnings);
            return base.HasWarnings() || warnings.Count > 0;
        }

        private void ValidateRecording(List<string> errors, List<string> warnings)
        {
            // No error detection here yet for image recorders
        }

        protected internal override bool HasErrors()
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            ValidateRecording(errors, warnings);
            return base.HasErrors() || errors.Count > 0;
        }

        protected internal override void GetErrors(List<string> errors)
        {
            base.GetErrors(errors);
            var warnings = new List<string>();
            ValidateRecording(errors, warnings);
        }

        protected internal override void GetWarnings(List<string> warnings)
        {
            base.GetWarnings(warnings);
            var errors = new List<string>();
            ValidateRecording(errors, warnings);
            if (CanCaptureAlpha() && captureAlpha)
            {
#if HDRP_AVAILABLE
                HdrpHelper.CheckRenderPipelineAssetAlphaSupport(warnings);
#endif
            }
        }

        bool IResolutionUser.IsOutputResolutionContradictory
        {
            get;
            set;
        }

        int IResolutionUser.OutputWidth => imageInputSettings.OutputWidth;

        int IResolutionUser.OutputHeight => imageInputSettings.OutputHeight;

        Type IResolutionUser.ImageInputType => imageInputSettings.GetType();
    }
}
