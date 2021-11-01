using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Media;
using UnityEditor.Media;
using UnityEditor.Recorder.Encoder;
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
        /// Use this property to define the encoder used by the Recorder.
        /// </summary>
        public IEncoderSettings EncoderSettings
        {
            get => encoderSettings;
            set => encoderSettings = value;
        }
        [SerializeReference] IEncoderSettings encoderSettings = new CoreEncoderSettings();

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
        /// Use this property to capture the audio signal (True) or not (False) in the output.
        /// </summary>
        /// <remarks>
        /// The audio signal will be captured only if the output format supports it.
        /// </remarks>
        public bool CaptureAudio
        {
            get => captureAudio;
            set
            {
                captureAudio = value;
                m_AudioInputSettings.PreserveAudio = value;
            }
        }
        [SerializeField] private bool captureAudio = true;

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
        /// Default constructor.
        /// </summary>
        public MovieRecorderSettings()
        {
            fileNameGenerator.FileName = DefaultWildcard.Recorder + "_" + DefaultWildcard.Take;
            FrameRate = 30.0f;

            var iis = m_ImageInputSelector.Selected as StandardImageInputSettings;
            if (iis != null)
                iis.maxSupportedSize = ImageHeight.x2160p_4K;

            // Force even resolution in GameView and Target Cam if required
            if (EncoderSettings.RequiresEvenResolution())
                m_ImageInputSelector.ForceEvenResolution(true);
        }

        /// <summary>
        /// Indicates the Image Input Settings currently used for this Recorder.
        /// </summary>
        public ImageInputSettings ImageInputSettings
        {
            get => m_ImageInputSelector.ImageInputSettings;
            set => m_ImageInputSelector.ImageInputSettings = value;
        }

        /// <summary>
        /// Indicates the Audio Input Settings currently used for this Recorder.
        /// </summary>
        public AudioInputSettings AudioInputSettings => m_AudioInputSettings;

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
        protected internal override string Extension => EncoderSettings.Extension;

        /// <inheritdoc/>
        internal override bool NeedToFlipVerticallyForOutputFormat =>
            EncoderSettings.CoordinateConvention != EncoderCoordinateConvention.OriginIsBottomLeft;

        internal void ValidateRecording(RecordingContext ctx, List<string> errors, List<string> warnings)
        {
            encoderSettings.ValidateRecording(ctx, errors, warnings);
        }

        /// <summary>
        /// Returns a RecordingContext for the current settings.
        /// </summary>
        /// <returns>A RecordingContext populated with fields that the MovieRecorderSettings controls.</returns>
        /// <remarks>
        /// Not all fields of the RecordingContext are populated (e.g. path).
        /// </remarks>
        /// <exception cref="InvalidCastException">Thrown if the input is not recognized.</exception>
        internal RecordingContext GetRecordingContext()
        {
            RecordingContext ctx = default;
            ctx.frameRateMode = FrameRatePlayback;
            ctx.doCaptureAlpha = ImageInputSettings.SupportsTransparent && EncoderSettings.CanCaptureAlpha && CaptureAlpha;
            ctx.doCaptureAudio = EncoderSettings.CanCaptureAudio && CaptureAudio && !UnityHelpers.CaptureAccumulation(this);
            ctx.fps = MovieRecorder.RationalFromDouble(FrameRate);
            var inputSettings = InputsSettings.First();
            if (inputSettings is GameViewInputSettings gvs)
            {
                ctx.height = gvs.OutputHeight;
                ctx.width = gvs.OutputWidth;
            }
            else if (inputSettings is CameraInputSettings cs)
            {
                ctx.height = cs.OutputHeight;
                ctx.width = cs.OutputWidth;
            }
            else if (inputSettings is Camera360InputSettings cs3)
            {
                ctx.height = cs3.OutputHeight;
                ctx.width = cs3.OutputWidth;
            }
            else if (inputSettings is RenderTextureInputSettings rts)
            {
                ctx.height = rts.OutputHeight;
                ctx.width = rts.OutputWidth;
            }
            else if (inputSettings is RenderTextureSamplerSettings ss)
            {
                ctx.height = ss.OutputHeight;
                ctx.width = ss.OutputWidth;
            }
            else
            {
                throw new InvalidCastException($"Unexpected type of input settings");
            }

            return ctx;
        }

        internal override bool HasWarnings()
        {
            var data = GetRecordingContext();
            var errors = new List<string>();
            var warnings = new List<string>();
            ValidateRecording(data, errors, warnings);
            return base.HasWarnings() || warnings.Count > 0 || errors.Count > 0;
        }

        protected internal override void GetWarnings(List<string> warnings)
        {
            base.GetWarnings(warnings);
        }

        protected internal override void GetErrors(List<string> errors)
        {
            base.GetErrors(errors);
        }

        // Obsolete and asset upgrade stuff. Should be moved to a new file (Trunk bug prevents it for now)


#pragma warning disable 618
        /// <summary>
        /// Available options for encoders to register the formats they support.
        /// </summary>
        [Obsolete("Please use the EncoderSettings API to access the format/codec of each Encoder.")]
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
        [Obsolete("Please use the EncoderSettings API to access the encoding quality of each Encoder.")]
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
        [Obsolete("Please use the EncoderSettings API to set/get the format/codec of each Encoder.")]
        public VideoRecorderOutputFormat OutputFormat
        {
            get
            {
                if (EncoderSettings is ProResEncoderSettings)
                {
                    return VideoRecorderOutputFormat.MOV;
                }

                if (EncoderSettings is CoreEncoderSettings settings)
                {
                    return (VideoRecorderOutputFormat)settings.Codec;
                }

                throw new Exception("Available only for CoreEncoderSettings and ProResEncoderSettings");
            }
            set
            {
                if (value == VideoRecorderOutputFormat.MOV)
                {
                    if (EncoderSettings is ProResEncoderSettings)
                    {
                        return;
                    }

                    if (!HasDefaultCoreEncoderSettings())
                    {
                        throw new Exception(
                            "CoreEncoderSettings already changed. Please use the EncoderSettings API");
                    }

                    EncoderSettings = new ProResEncoderSettings();
                    return;
                }

                if (EncoderSettings is CoreEncoderSettings || HasDefaultProResEncoderSettings())
                {
                    CoreEncoderSettings s;
                    if (EncoderSettings is CoreEncoderSettings)
                    {
                        s = EncoderSettings as CoreEncoderSettings;
                    }
                    else
                    {
                        s = new CoreEncoderSettings();
                    }
                    s.Codec = value == VideoRecorderOutputFormat.MP4
                        ? CoreEncoderSettings.OutputCodec.MP4
                        : CoreEncoderSettings.OutputCodec.WEBM;

                    EncoderSettings = s;
                }
                else
                {
                    // Do not silently wipe proRes settings
                    throw new Exception("ProResEncoderSettings already changed. Please use the EncoderSettings API");
                }
            }
        }

        [SerializeField] VideoRecorderOutputFormat outputFormat = VideoRecorderOutputFormat.MP4;

        /// <summary>
        /// Indicates the video bit rate preset currently used for this Recorder.
        /// </summary>
        [Obsolete("Please use the EncoderSettings API to set/get the bitrate/encoding quality of each Encoder.")]
        public VideoBitrateMode VideoBitRateMode
        {
            get
            {
                if (EncoderSettings is CoreEncoderSettings settings)
                {
                    return ConvertBitrateMode((VideoEncodingQuality)settings.EncodingQuality);
                }

                throw new Exception("Available only for CoreEncoderSettings");
            }
            set
            {
                if (EncoderSettings is CoreEncoderSettings settings)
                {
                    settings.EncodingQuality = (CoreEncoderSettings.VideoEncodingQuality)value;
                }
                else if (HasDefaultProResEncoderSettings())
                {
                    var s = new CoreEncoderSettings
                    {
                        EncodingQuality = (CoreEncoderSettings.VideoEncodingQuality)value
                    };
                    EncoderSettings = s;
                }
                else
                    throw new Exception("Available only for CoreEncoderSettings");
            }
        }

        /// <summary>
        /// Indicates the encoding quality to use for this Recorder.
        /// </summary>
        [Obsolete("Please use the EncoderSettings API to set/get the bitrate/encoding quality of each Encoder.")]
        public VideoEncodingQuality EncodingQuality
        {
            get
            {
                if (EncoderSettings is CoreEncoderSettings settings)
                {
                    return (VideoEncodingQuality)settings.EncodingQuality;
                }

                throw new Exception("Available only for CoreEncoderSettings");
            }
            set
            {
                if (EncoderSettings is CoreEncoderSettings settings)
                {
                    settings.EncodingQuality = (CoreEncoderSettings.VideoEncodingQuality)value;
                }
                else
                {
                    throw new Exception("Available only for CoreEncoderSettings");
                }
            }
        }

        [SerializeField, FormerlySerializedAs("videoBitRateMode")]
        VideoEncodingQuality encodingQuality = VideoEncodingQuality.High;

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

        internal override void OnUpgradeFromVersion(Versions oldVersion)
        {
            if (oldVersion < Versions.MovieEncoders)
            {
                IEncoderSettings settings;
                if (outputFormat == VideoRecorderOutputFormat.MOV)
                {
                    settings = new ProResEncoderSettings
                    {
                        Format = (ProResEncoderSettings.OutputFormat)(encoderPresetSelected)
                    };
                }
                else
                {
                    settings = new CoreEncoderSettings
                    {
                        Codec = outputFormat == VideoRecorderOutputFormat.MP4 ? CoreEncoderSettings.OutputCodec.MP4 : CoreEncoderSettings.OutputCodec.WEBM,
                        EncodingQuality = (CoreEncoderSettings.VideoEncodingQuality)encodingQuality,
                    };
                }

                EncoderSettings = settings;
            }
        }

        bool HasDefaultCoreEncoderSettings()
        {
            return EncoderSettings == null ||
                EncoderSettings is CoreEncoderSettings && EncoderSettings.Equals(new CoreEncoderSettings());
        }

        bool HasDefaultProResEncoderSettings()
        {
            return EncoderSettings == null ||
                EncoderSettings is ProResEncoderSettings && EncoderSettings.Equals(new ProResEncoderSettings());
        }

#pragma warning restore 618
    }
}
