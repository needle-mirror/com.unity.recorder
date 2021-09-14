using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using ProResOut;
using Unity.Media;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.Serialization;

[assembly: InternalsVisibleTo("Unity.Recorder.TestsCodebase")]
namespace UnityEditor.Recorder
{
    /// <summary>
    /// A class that represents the settings of a Movie Recorder.
    /// </summary>
    [RecorderSettings(typeof(MovieRecorder), "Movie", "movie_16")]
    public class MovieRecorderSettings : RecorderSettings, IAccumulation
    {
        /// <summary>
        /// Available options for encoders to register the formats they support.
        /// </summary>
        public enum VideoRecorderOutputFormat
        {
            /// <summary>
            /// Output the recording with the H.264 codec in an MP4 container.
            /// </summary>
            [InspectorName("H.264 MP4")] MP4,
            /// <summary>
            /// Output the recording with the VP9 codec in a WebM container.
            /// </summary>
            [InspectorName("VP8 WebM")] WebM,
            /// <summary>
            /// Output the recording with the ProRes codec in a MOV container.
            /// </summary>
            [InspectorName("ProRes QuickTime")] MOV,
        }

        /// <summary>
        /// Available options for the encoding quality of videos.
        /// </summary>
        public enum VideoEncodingQuality
        {
            /// <summary>
            /// Low value, safe for slower internet connections or clips where visual quality is not critical.
            /// </summary>
            Low,
            /// <summary>
            /// Typical bit rate supported by internet connections.
            /// </summary>
            Medium,
            /// <summary>
            /// High value, possibly exceeding typical internet connection capabilities.
            /// </summary>
            High
        }

        /// <summary>
        /// Indicates the output video format currently used for this Recorder.
        /// </summary>
        public VideoRecorderOutputFormat OutputFormat
        {
            get { return outputFormat; }
            set { outputFormat = value; }
        }

        [SerializeField] VideoRecorderOutputFormat outputFormat = VideoRecorderOutputFormat.MP4;

        /// <summary>
        /// Indicates the video bit rate preset currently used for this Recorder.
        /// </summary>
        [Obsolete("Please use property 'EncodingQuality'.")]
        public VideoBitrateMode VideoBitRateMode
        {
            get => ConvertBitrateMode(encodingQuality);
            set
            {
                switch (value)
                {
                    case VideoBitrateMode.High:
                        encodingQuality = VideoEncodingQuality.High;
                        break;
                    case VideoBitrateMode.Medium:
                        encodingQuality = VideoEncodingQuality.Medium;
                        break;
                    case VideoBitrateMode.Low:
                        encodingQuality = VideoEncodingQuality.Low;
                        break;
                    default:
                        throw new InvalidEnumArgumentException($"Unexpected video bitrate mode value '{value}'.");
                }
            }
        }

        /// <summary>
        /// Indicates the encoding quality to use for this Recorder.
        /// </summary>
        public VideoEncodingQuality EncodingQuality
        {
            get { return encodingQuality; }
            set { encodingQuality = value; }
        }

        [SerializeField, FormerlySerializedAs("videoBitRateMode")] private VideoEncodingQuality encodingQuality = VideoEncodingQuality.High;

        /// <summary>
        /// Use this property to capture the alpha channel (True) or not (False) in the output.
        /// </summary>
        /// <remarks>
        /// Alpha channel will be captured only if the output image format supports it.
        /// </remarks>
        public bool CaptureAlpha
        {
            get { return captureAlpha; }
            set { captureAlpha = value; }
        }

        [SerializeField] private bool captureAlpha;

        /// <summary>
        /// The list of registered encoders.
        /// </summary>
        [SerializeReference]
        internal List<MediaEncoderRegister> encodersRegistered;

        /// <summary>
        /// Gets the preset names and options of the encoder at the specified index.
        /// </summary>
        /// <param name="indexEncoder">The index of the encoder to query</param>
        /// <param name="presetNames">The list of preset names</param>
        /// <param name="presetOptions">The list of preset options</param>
        public void GetPresetsForEncoder(int indexEncoder, out List<string> presetNames, out List<string> presetOptions)
        {
            encodersRegistered[indexEncoder].GetPresets(out presetNames, out presetOptions);
        }

        /// <summary>
        /// Gets the currently selected encoder.
        /// </summary>
        /// <returns></returns>
        internal MediaEncoderRegister GetCurrentEncoder()
        {
            return encodersRegistered[encoderSelected];
        }

        /// <summary>
        /// Returns true if and only if the settings mean to capture transparency, the input source supports it, and the
        /// codec preset also allows it.
        /// </summary>
        /// <returns></returns>
        internal bool WillIncludeAlpha()
        {
            // GameViewInput does not support transparency
            var codecFormat = (ProResOut.ProResCodecFormat)encoderPresetSelected;
            bool codecFormatSupportsTransparency = ProResPresetExtensions.CodecFormatSupportsTransparency(codecFormat);
            return CaptureAlpha && !(ImageInputSettings is GameViewInputSettings) && codecFormatSupportsTransparency;
        }

        /// <summary>
        /// Destroy the specified handle if it is already present.
        /// </summary>
        /// <param name="handle"></param>
        internal void DestroyIfExists(MediaEncoderHandle handle)
        {
            if (m_EncoderManager.Exists(handle))
                m_EncoderManager.Destroy(handle);
        }

        internal MediaEncoderManager m_EncoderManager = new MediaEncoderManager();

        /// <summary>
        /// The index of the currently selected container format in the list of formats that the registered encoders support.
        /// </summary>
        [SerializeField, UsedImplicitly] internal int containerFormatSelected = 0;

        /// <summary>
        /// The index of the currently selected encoder in the list of registered encoders.
        /// </summary>
        [SerializeField, UsedImplicitly] internal int encoderSelected = 0;

        /// <summary>
        /// The index of the preset selected for the current encoder, when the encoder supports several presets (i.e., codec formats).
        /// </summary>
        [SerializeField, UsedImplicitly] internal int encoderPresetSelected = 0;

        /// <summary>
        /// The name of the currently selected encoder preset.
        /// </summary>
        [SerializeField, UsedImplicitly] internal string encoderPresetSelectedName = "";

        /// <summary>
        /// The custom options of the currently selected encoder preset.
        /// </summary>
        [SerializeField, UsedImplicitly] internal string encoderPresetSelectedOptions = "";

        /// <summary>
        /// The extension (without leading dot) of the files created by the currently selected encoder preset.
        /// </summary>
        [SerializeField, UsedImplicitly] internal string encoderPresetSelectedSuffixes = "";

        /// <summary>
        /// The index of the color definition selected for the current encoder, when the encoder supports color definition.
        /// </summary>
        [SerializeField, UsedImplicitly] internal int encoderColorDefinitionSelected = 0;

        /// <summary>
        /// Some custom options that are specified for the currently selected encoder.
        /// </summary>
        [SerializeField, UsedImplicitly] internal string encoderCustomOptions = "";

        [SerializeField] ImageInputSelector m_ImageInputSelector = new ImageInputSelector();
        [SerializeField] AudioInputSettings m_AudioInputSettings = new AudioInputSettings();

        [SerializeReference] AccumulationSettings _accumulationSettings = new AccumulationSettings();

        /// <summary>
        /// Stores the AccumulationSettings properties.
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

        /// <summary>
        /// These are attributes that are exposed to the Recorder, for customization.
        /// </summary>
        internal enum MovieRecorderSettingsAttributes
        {
            CodecFormat,        // for encoders that support multiple formats (e.g. ProRes 4444XQ vs ProRes 422)
            CustomOptions,      // for encoders that can have additional options (e.g. command-line arguments)
            ColorDefinition,    // for encoders that support different color definitions
        }

        internal static readonly Dictionary<MovieRecorderSettingsAttributes, string> AttributeLabels = new Dictionary<MovieRecorderSettingsAttributes, string>()
        {
            { MovieRecorderSettingsAttributes.CodecFormat, "CodecFormat" },
            { MovieRecorderSettingsAttributes.CustomOptions, "CustomOptions" },
            { MovieRecorderSettingsAttributes.ColorDefinition, "ColorDefinition" }
        };

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MovieRecorderSettings()
        {
            fileNameGenerator.FileName = DefaultWildcard.Recorder + "_" + DefaultWildcard.Take;
            FrameRate = 30;

#if UNITY_EDITOR_LINUX
            // Default to WebM, the only supported format
            OutputFormat = VideoRecorderOutputFormat.WebM;
            // For the inspector, make sure that the selected container matches the WebM
            containerFormatSelected = (int)VideoRecorderOutputFormat.WebM;
#endif

            var iis = m_ImageInputSelector.Selected as StandardImageInputSettings;
            if (iis != null)
                iis.maxSupportedSize = ImageHeight.x2160p_4K;

            m_ImageInputSelector.ForceEvenResolution(OutputFormat == VideoRecorderOutputFormat.MP4);
            RegisterAllEncoders();
        }

        /// <summary>
        /// Find all the encoders by looking at the content of the current assemblies.
        /// </summary>
        private void RegisterAllEncoders()
        {
            encodersRegistered = new List<MediaEncoderRegister>();
            // For all assemblies find all MediaEncoderRegister
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var allTypes = a.GetTypes();
                    var encoders = allTypes.Where(
                        type => type.IsSubclassOf(typeof(MediaEncoderRegister))
                    );
                    foreach (var e in encoders)
                    {
                        var o = Activator.CreateInstance(e);
                        var mr = o as MediaEncoderRegister;
                        encodersRegistered.Add(mr);
                    }
                }
                catch (ReflectionTypeLoadException e)
                {
                    Debug.LogWarning($"Failed to look for Movie Encoders in assembly '{a.FullName}': {e.Message}");
                }
            }
            // Enforce the alphabetical order of encoders so that CoreMediaEncoder is first and ProRes second, so that
            // their formats are processed in that order by the MovieRecorderEditor class
            encodersRegistered = encodersRegistered.OrderBy(a => a.GetName()).ToList();
        }

        /// <summary>
        /// Indicates the Image Input Settings currently used for this Recorder.
        /// </summary>
        public ImageInputSettings ImageInputSettings
        {
            get { return m_ImageInputSelector.ImageInputSettings; }
            set { m_ImageInputSelector.ImageInputSettings = value; }
        }

        /// <summary>
        /// Indicates the Audio Input Settings currently used for this Recorder.
        /// </summary>
        public AudioInputSettings AudioInputSettings
        {
            get { return m_AudioInputSettings; }
        }

        /// <inheritdoc/>
        public override IEnumerable<RecorderInputSettings> InputsSettings
        {
            get
            {
                yield return m_ImageInputSelector.Selected;
                yield return m_AudioInputSettings;
            }
        }

        /// <inheritdoc/>
        protected internal override string Extension
        {
            get
            {
                var encoders = encodersRegistered.ToArray();
                if (encoders[encoderSelected].GetType() == typeof(CoreMediaEncoderRegister))
                {
                    return OutputFormat.ToString().ToLower();
                }
                else
                {
                    return encoders[encoderSelected].GetDefaultExtension();
                }
            }
        }

        protected internal override void GetWarnings(List<string> warnings)
        {
            base.GetWarnings(warnings);

            var iis = m_ImageInputSelector.Selected as ImageInputSettings;
            if (iis != null)
            {
                string errorMsg, warningMsg;
                encodersRegistered[encoderSelected].SupportsResolution(this, iis.OutputWidth, iis.OutputHeight,
                    out errorMsg, out warningMsg);
                if (warningMsg != String.Empty)
                    warnings.Add(warningMsg);
            }
        }

        protected internal override void GetErrors(List<string> errors)
        {
            base.GetErrors(errors);

            string errorMsg;
            if (FrameRatePlayback == FrameRatePlayback.Variable && !encodersRegistered[encoderSelected].SupportsVFR(this, out errorMsg))
                errors.Add(errorMsg);

            var iis = m_ImageInputSelector.Selected as ImageInputSettings;
            if (iis != null)
            {
                string warningMsg;
                if (!encodersRegistered[encoderSelected].SupportsResolution(this, iis.OutputWidth, iis.OutputHeight, out errorMsg, out warningMsg))
                    errors.Add(errorMsg);

                if (encodersRegistered[encoderSelected].GetSupportedFormats() == null || !encodersRegistered[encoderSelected].GetSupportedFormats().Contains(OutputFormat))
                    errors.Add($"Format '{OutputFormat}' is not supported on this platform.");
            }
        }

        internal override void SelfAdjustSettings()
        {
            var selectedInput = m_ImageInputSelector.Selected;
            if (selectedInput == null)
                return;

            var iis = selectedInput as StandardImageInputSettings;

            if (iis != null)
            {
                iis.maxSupportedSize = OutputFormat == VideoRecorderOutputFormat.MP4
                    ? ImageHeight.x2160p_4K
                    : ImageHeight.x4320p_8K;

                if (iis.outputImageHeight != ImageHeight.Window && iis.outputImageHeight != ImageHeight.Custom)
                {
                    if (iis.outputImageHeight > iis.maxSupportedSize)
                        iis.outputImageHeight = iis.maxSupportedSize;
                }
            }

            var cbis = selectedInput as ImageInputSettings;
            if (cbis != null)
            {
                var encoder = encodersRegistered[encoderSelected];
                if (encoder is ProResEncoderRegister p)
                {
                    var codecFormat = (ProResOut.ProResCodecFormat)encoderPresetSelected;
                    bool codecFormatSupportsTransparency = ProResPresetExtensions.CodecFormatSupportsTransparency(codecFormat);
                    cbis.RecordTransparency = CaptureAlpha && codecFormatSupportsTransparency;
                }
                else
                {
                    switch (OutputFormat)
                    {
                        case VideoRecorderOutputFormat.WebM:
                            cbis.RecordTransparency = CaptureAlpha;
                            break;
                        case VideoRecorderOutputFormat.MP4:
                        default:
                            cbis.RecordTransparency = false;
                            break;
                    }
                }
            }

            m_ImageInputSelector.ForceEvenResolution(OutputFormat == VideoRecorderOutputFormat.MP4);
        }

        /// <summary>
        /// A method that converts from a Recorder enum value to a core engine enum value.
        /// </summary>
        /// <param name="quality">The enum value to convert.</param>
        /// <returns>The converted core engine enum value.</returns>
        /// <exception cref="InvalidEnumArgumentException">Throws an exception if the passed value is unexpected</exception>
        internal static VideoBitrateMode ConvertBitrateMode(VideoEncodingQuality quality)
        {
            switch (quality)
            {
                case VideoEncodingQuality.Low:
                    return VideoBitrateMode.Low;
                case VideoEncodingQuality.Medium:
                    return VideoBitrateMode.Medium;
                case VideoEncodingQuality.High:
                    return VideoBitrateMode.High;
                default:
                    throw new InvalidEnumArgumentException($"Unexpected VideoEncodingQuality value '{quality}'");
            }
        }
    }
}
