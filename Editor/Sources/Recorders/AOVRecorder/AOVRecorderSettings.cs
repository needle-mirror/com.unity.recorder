using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace UnityEditor.Recorder
{
    /// <summary>
    /// Available options AOV Types.
    /// </summary>
    public enum AOVType
    {
        /// <summary>
        /// Select the Beauty AOV.
        /// </summary>
        Beauty = 0,
        /// <summary>
        /// Select the Albedo AOV.
        /// </summary>
        Albedo = 1,
        /// <summary>
        /// Select the Normal AOV.
        /// </summary>
        Normal = 2,
        /// <summary>
        /// Select the Smootness AOV.
        /// </summary>
        Smoothness = 3,
        /// <summary>
        /// Select the Ambient Occlusion AOV.
        /// </summary>
        AmbientOcclusion = 4,
        /// <summary>
        /// Select the Metal AOV.
        /// </summary>
        Metal = 5,
        /// <summary>
        /// Select the Specular AOV.
        /// </summary>
        Specular = 6,
        /// <summary>
        /// Select the Alpha AOV.
        /// </summary>
        Alpha = 7,
        /// <summary>
        /// Select the Direct Diffuse Lighting Only AOV.
        /// </summary>
        DirectDiffuse = 8,
        /// <summary>
        /// Select the Direct Specular Lighting Only AOV.
        /// </summary>
        DirectSpecular = 9,
        /// <summary>
        /// Select the Indirect Diffuse Lighting Only AOV.
        /// </summary>
        IndirectDiffuse = 10,
        /// <summary>
        /// Select the Reflection Lighting Only AOV.
        /// </summary>
        Reflection = 11,
        /// <summary>
        /// Select the Refraction Lighting Only AOV.
        /// </summary>
        Refraction = 12,
        /// <summary>
        /// Select the Emissive Only AOV.
        /// </summary>
        Emissive = 13,
        /// <summary>
        /// Select the Motion Vector AOV.
        /// </summary>
        MotionVectors = 14,
        /// <summary>
        /// Select the Depth AOV.
        /// </summary>
        Depth = 15
    }

    /// <summary>
    /// A class that represents the settings of an AOV Sequence Recorder.
    /// </summary>
    [RecorderSettings(typeof(AOVRecorder), "AOV Image Sequence", "aovimagesequence_16")]
    public class AOVRecorderSettings : RecorderSettings, RecorderSettings.IResolutionUser
    {
        [SerializeField] internal ImageRecorderSettings.ImageRecorderOutputFormat m_OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.EXR;

        // Deprecated in favor of m_AOVMultiSelection, still needs to be there to upgrade old recorder settings
        [SerializeField] private AOVType m_AOVSelection;
        [SerializeField] List<AOVType> m_AOVMultiSelection = new List<AOVType>() { AOVType.Beauty };

        [SerializeField] CompressionUtility.EXRCompressionType m_EXRCompression = CompressionUtility.EXRCompressionType.Zip;
        [SerializeField] int m_EXRCompressionLevel = CompressionUtility.DWACompressionTypeInfo.k_DefaultValue;
        [SerializeField] bool m_IsMultiPartEXR = true;

        [SerializeField] internal AOVImageInputSelector m_AOVImageInputSelector = new AOVImageInputSelector();
        [SerializeField] internal ImageRecorderSettings.ColorSpaceType m_ColorSpace = ImageRecorderSettings.ColorSpaceType.Unclamped_linear_sRGB;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AOVRecorderSettings()
        {
            var aovWildcard = DefaultWildcard.GeneratePattern("AOV");
            FileNameGenerator.FileName = DefaultWildcard.Recorder + "_" + aovWildcard + "_" + DefaultWildcard.Take + "_" + DefaultWildcard.Frame;
            FileNameGenerator.AddWildcard(aovWildcard, AOVNameResolver);
        }

        internal enum AovVersions
        {
            Initial = 0,
            MultiPartEXR = 1,
        }
        [SerializeField, HideInInspector] internal int m_AovVersion = 0;

        /// <inheritdoc />
        protected override int Version
        {
            get => m_AovVersion;
            set => m_AovVersion = value;
        }

        /// <inheritdoc />
        protected override int LatestVersion => (int)AovVersions.MultiPartEXR;

        /// <inheritdoc />
        protected override void OnUpgradeFromVersion()
        {
            // DirectDiffuse and DirectSpecular were deprecated in the multi-selection version of the AOVRecorder
            var deprecatedAovValues = new int[] { 8 , 9 };
            m_AOVMultiSelection = new List<AOVType>();

            // Ignore the selection if it's a deprecated value
            if (deprecatedAovValues.Contains((int)m_AOVSelection)) return;

            if ((int)m_AOVSelection > 7)
            {
                // Elements with enum value greater than 7 are shifted down by 2 as of the multi-part version.
                m_AOVMultiSelection.Add(m_AOVSelection - 2);
            }
            else
            {
                m_AOVMultiSelection.Add(m_AOVSelection);
            }
        }

        string AOVNameResolver(RecordingSession session)
        {
            var aovSelection  = GetAOVSelection();

            if (aovSelection.Length == 0)
            {
                return "";
            }

            if (aovSelection.Length == 1 || !IsMultiPartEXR)
            {
                return aovSelection.First().ToString();
            }

            return "multiAOV";
        }

        /// <inheritdoc/>
        protected internal override string Extension
        {
            get
            {
                switch (m_OutputFormat)
                {
                    case ImageRecorderSettings.ImageRecorderOutputFormat.PNG:
                        return "png";
                    case ImageRecorderSettings.ImageRecorderOutputFormat.JPEG:
                        return "jpg";
                    case ImageRecorderSettings.ImageRecorderOutputFormat.EXR:
                        return "exr";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Indicates the selected AOVs to render.
        /// </summary>
        /// <returns>
        /// The array of selected AOVTypes.
        /// </returns>>
        public AOVType[] GetAOVSelection()
        {
            return m_AOVMultiSelection.ToArray();
        }

        /// <summary>
        /// Indicates the selected AOVs to render.
        /// </summary>
        ///
        /// <param name="value">The array of AOVTypes to select.</param>
        public void SetAOVSelection(params AOVType[] value)
        {
            m_AOVMultiSelection.Clear();
            foreach (var aovType in value)
            {
                if (aovType == AOVType.Beauty)
                {
                    m_AOVMultiSelection.Insert(0, aovType);
                }
                else
                {
                    m_AOVMultiSelection.Add(aovType);
                }
            }
        }

        /// <summary>
        /// Indicates the selected AOV to render.
        /// </summary>
        [Obsolete("Use Get/SetAOVSelection to choose the AOVs")]
        public AOVType AOVSelection
        {
            get => GetAOVSelection().First();
            set => SetAOVSelection(value);
        }

        /// <summary>
        /// Stores the color space to use to encode the output image files.
        /// </summary>
        public ImageRecorderSettings.ColorSpaceType OutputColorSpace
        {
            get => m_ColorSpace;
            set => m_ColorSpace = value;
        }

        /// <summary>
        /// Determines how a recorded frame is exported in EXR.
        /// If true, the recorder will write all exported AOVs to a single multi-part file.
        /// If false, the recorder will write each AOV to its own EXR file.
        /// </summary>
        public bool IsMultiPartEXR
        {
            get => OutputFormat == ImageRecorderSettings.ImageRecorderOutputFormat.EXR && m_IsMultiPartEXR;
            set => m_IsMultiPartEXR = value;
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
        /// Stores the data compression level for compression methods that support it to encode image files in the EXR format.
        /// </summary>
        public int EXRCompressionLevel
        {
            get => m_EXRCompressionLevel;
            set => m_EXRCompressionLevel = value;
        }

        internal bool CanCaptureHDRFrames()
        {
            bool isFormatExr = m_OutputFormat == ImageRecorderSettings.ImageRecorderOutputFormat.EXR;
            return isFormatExr;
        }

        /// <summary>
        /// Stores the output image format currently used for this Recorder.
        /// </summary>
        public ImageRecorderSettings.ImageRecorderOutputFormat OutputFormat
        {
            get { return m_OutputFormat; }
            set { m_OutputFormat = value; }
        }

        /// <summary>
        /// Use this property to capture the frames in HDR (if the setup supports it).
        /// </summary>
        public bool CaptureHDR
        {
            get => CanCaptureHDRFrames() && m_ColorSpace == ImageRecorderSettings.ColorSpaceType.Unclamped_linear_sRGB;
        }

        /// <summary>
        /// The settings of the input image.
        /// </summary>
        public ImageInputSettings imageInputSettings
        {
            get { return m_AOVImageInputSelector.imageInputSettings; }
            set { m_AOVImageInputSelector.imageInputSettings = value; }
        }

        /// <summary>
        /// Add AOV related error description strings to the errors list.
        /// </summary>
        /// <param name="errors">A list of error message strings.</param>
        protected internal override void GetErrors(List<string> errors)
        {
            base.GetErrors(errors);

            if (imageInputSettings.OutputWidth % 4 != 0)
            {
                // RGBA->RGB shaders are tricky to write for uncommon cases. We could implement a slower CPU-based solution if this resolution restriction becomes an issue.
                errors.Add("The resolution width must be a multiple of 4");
            }

            if (!GetAOVSelection().Any())
            {
                errors.Add("At least one AOV must be selected");
            }

            if (OutputFormat == ImageRecorderSettings.ImageRecorderOutputFormat.JPEG)
            {
                errors.Add("JPEG is not a valid output format");
            }
#if !HDRP_AVAILABLE
            errors.Add("This feature is only compatible with HDRP");
#endif
        }

#if HDRP_AVAILABLE
        // Return true if and only if the selected AOV is valid in the current context
        private bool IsAOVSelectionValid()
        {
            return GetAOVSelection().All(AOVCameraAOVRequestAPIInput.AOVInfoLookUp.Keys.Contains);
        }

#endif

        internal override bool IsInvalid()
        {
#if HDRP_AVAILABLE
            // Is the selection supported?
            return !IsAOVSelectionValid();
#else
            var hdrpMinVersion = 6.6; // Since 2019.2
            Enabled = false;
            Debug.LogError("AOV Recorder requires the HDRP package version " + hdrpMinVersion + " or greater to be installed");
            return true;
#endif
        }

        /// <summary>
        /// The list of settings of the Recorder Inputs.
        /// </summary>
        public override IEnumerable<RecorderInputSettings> InputsSettings
        {
            get { yield return m_AOVImageInputSelector.Selected; }
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
