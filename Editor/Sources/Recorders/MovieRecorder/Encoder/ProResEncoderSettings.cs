using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Recorder.Encoder
{
    /// <summary>
    /// The settings of the ProRes Encoder.
    /// </summary>
    /// <remarks>
    /// This class is sealed because users shouldn't inherit from it. Instead, create a new encoder along with its settings class.
    /// </remarks>
    [DisplayName("ProRes Encoder")]
    [Serializable]
    [EncoderSettings(typeof(ProResEncoder))]
    public sealed class ProResEncoderSettings : IEncoderSettings, IEquatable<ProResEncoderSettings>
    {
        /// <summary>
        /// The output format of the ProRes encoder.
        /// </summary>
        public enum OutputFormat
        {
            /// <summary>
            /// The 4444 XQ ProRes codec format, identified by four-character code ap4x.
            /// </summary>
            [InspectorName("Apple ProRes 4444 XQ (ap4x)")] ProRes4444XQ,
            /// <summary>
            /// The 4444 ProRes codec format, identified by four-character code ap4h.
            /// </summary>
            [InspectorName("Apple ProRes 4444 (ap4h)")] ProRes4444,
            /// <summary>
            /// The 422 HQ ProRes codec format, identified by four-character code apch.
            /// </summary>
            [InspectorName("Apple ProRes 422 HQ (apch)")] ProRes422HQ,
            /// <summary>
            /// The 422 ProRes codec format, identified by four-character code apcn.
            /// </summary>
            [InspectorName("Apple ProRes 422 (apcn)")] ProRes422,
            /// <summary>
            /// The 422 LT ProRes codec format, identified by four-character code apcs.
            /// </summary>
            [InspectorName("Apple ProRes 422 LT (apcs)")] ProRes422LT,
            /// <summary>
            /// The 422 Proxy ProRes codec format, identified by four-character code apco.
            /// </summary>
            [InspectorName("Apple ProRes 422 Proxy (apco)")] ProRes422Proxy
        }

        /// <summary>
        /// The list of available color definitions.
        /// </summary>
        public enum ProResColorDefinition
        {
            // Most values are not exposed yet.
            //SD_Rec601_525_60Hz = 0,
            //SD_Rec601_625_50Hz = 1,

            /// <summary>
            /// The Rec. 709 color space.
            /// </summary>
            HD_Rec709 = 2,

            //Rec2020 = 3,
            //HDR_SMPTE_ST_2084_Rec2020 = 4,
            //HDR_HLG_Rec2020 = 5
        }

        /// <summary>
        /// The format of the encoder.
        /// </summary>
        public OutputFormat Format
        {
            get => outputFormat;
            set => outputFormat = value;
        }
        [SerializeField] OutputFormat outputFormat;

        /// <inheritdoc/>
        string IEncoderSettings.Extension => "mov";

        /// <inheritdoc/>
        bool IEncoderSettings.CanCaptureAlpha => CodecFormatSupportsAlphaChannel(Format);

        /// <inheritdoc/>
        bool IEncoderSettings.CanCaptureAudio => true;

        internal readonly AudioSpeakerMode[] kSupportedSpeakerModes = new AudioSpeakerMode[] { AudioSpeakerMode.Stereo};

        /// <summary>
        /// Indicates whether the requested ProRes codec format can encode an alpha channel or not.
        /// </summary>
        /// <param name="format">The ProRes codec format to check.</param>
        /// <returns>True if the specified codec can encode an alpha channel, False otherwise.</returns>
        internal bool CodecFormatSupportsAlphaChannel(OutputFormat format)
        {
            return format == OutputFormat.ProRes4444XQ || format == OutputFormat.ProRes4444;
        }

        /// <inheritdoc/>
        TextureFormat IEncoderSettings.GetTextureFormat(bool inputContainsAlpha)
        {
            var codecFormatSupportsTransparency = CodecFormatSupportsAlphaChannel(Format);
            var willIncludeAlpha = inputContainsAlpha && codecFormatSupportsTransparency;
            var formatIs4444Or4444XQ = Format == OutputFormat.ProRes4444 || Format == OutputFormat.ProRes4444XQ;
            return willIncludeAlpha || formatIs4444Or4444XQ ? TextureFormat.RGBA64 : TextureFormat.RGB24;
        }

        /// <inheritdoc/>
        void IEncoderSettings.ValidateRecording(RecordingContext ctx, List<string> errors, List<string> warnings)
        {
            // Is the codec format supported?
            if (!IsOutputFormatSupported(Format))
                errors.Add($"Format '{Format}' is not supported on this platform.");

            if (ctx.doCaptureAlpha && !CodecFormatSupportsAlphaChannel(Format))
                errors.Add($"Format '{Format}' does not support transparency.");

            if (ctx.frameRateMode == FrameRatePlayback.Variable)
                errors.Add($"This encoder does not support Variable frame rate playback. Please consider using Constant frame rate instead.");

            // For packed pixel formats (2yuv), refuse odd resolutions
            if (Format is OutputFormat.ProRes422 or OutputFormat.ProRes422Proxy or OutputFormat.ProRes422LT or OutputFormat.ProRes422HQ)
            {
                if (ctx.height % 2 != 0 || ctx.width % 2 != 0)
                    errors.Add($"The {Format} format does not support odd values in resolution: {ctx.height}x{ctx.width}");
            }

            // https://jira.unity3d.com/projects/REC/issues/REC-1128
            if (ctx.doCaptureAudio && AudioSettings.speakerMode is not AudioSpeakerMode.Stereo)
                errors.Add(UnityHelpers.GetUnsupportedSpeakerModeErrorMessage("ProRes Encoder", kSupportedSpeakerModes));
        }

        /// <inheritdoc/>
        public bool SupportsCurrentPlatform()
        {
#if UNITY_EDITOR_LINUX
            return false;
#else
            return true;
#endif
        }

        /// <summary>
        /// Indicates whether the specified ProRes codec format is supported on the current operating system or not.
        /// </summary>
        /// <param name="toCheck">The ProRes codec format to check.</param>
        /// <returns>True if the specified output format is supported on the current operating system, False otherwise</returns>
        /// <remarks>
        /// On Windows, all formats are available.
        /// macOS 10.13+ is required for ProRes codec formats 4444 and 422.
        /// macOS 10.15+ is required for ProRes codec formats 4444 XQ, 422 HQ, 422 LT, and 422 Proxy.
        /// </remarks>
        internal static bool IsOutputFormatSupported(OutputFormat toCheck)
        {
#if UNITY_EDITOR_OSX
            // Ensure that this codec format is supported, because on macOS we depend on AVFoundation in the OS
            System.Text.StringBuilder sb = new System.Text.StringBuilder(128);
            bool supported = ProResWrapperHelpers.SupportsCodecFormat((int)toCheck, sb, sb.Capacity);
            return supported;
#else
            return true;
#endif
        }

        /// <inheritdoc/>
        bool IEquatable<ProResEncoderSettings>.Equals(ProResEncoderSettings other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return outputFormat == other.outputFormat;
        }

        /// <summary>
        /// Compares the current object with another one.
        /// </summary>
        /// <param name="obj">The object to compare with the current one.</param>
        /// <returns>True if the two objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is ProResEncoderSettings other && ((IEquatable<ProResEncoderSettings>) this).Equals(other);
        }

        /// <summary>
        /// Returns a hash code of all serialized fields.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine((int)outputFormat);
        }
    }
}
