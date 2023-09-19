using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace UnityEditor.Recorder.Encoder
{
    /// <summary>
    /// The settings of the Core Encoder.
    /// </summary>
    /// <remarks>
    /// This class is sealed because users shouldn't inherit from it. Instead, create a new encoder along with its settings class.
    /// </remarks>
    [DisplayName("Unity Media Encoder")]
    [Serializable]
    [EncoderSettings(typeof(CoreEncoder))]
    public sealed class CoreEncoderSettings : IEncoderSettings, IEquatable<CoreEncoderSettings>
    {
        internal readonly int kMaxSupportedSize_H264 = (int)ImageHeight.x2160p_4K;
        internal readonly int kMaxSupportedSize_VP8 = (int)ImageHeight.x4320p_8K;
        internal readonly int kMaxSupportedBitrate = 4150; // Mbps
        internal readonly AudioSpeakerMode[] kSupportedSpeakerModes = new AudioSpeakerMode[] { AudioSpeakerMode.Mono , AudioSpeakerMode.Stereo};

        /// <summary>
        /// The choice of encoder and container for the output file.
        /// </summary>
        public enum OutputCodec
        {
            /// <summary>
            /// The H.264 codec in an MPEG-4 container.
            /// </summary>
            [InspectorName("H.264 MP4")] MP4 = 0,

            /// <summary>
            /// The VP8 codec in a WebM container.
            /// </summary>
            [InspectorName("VP8 WebM")] WEBM
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
            High,
            /// <summary>
            /// Advanced settings for encoding video with custom quality.
            /// </summary>
            Custom
        }

        /// <summary>
        /// The available encoding profiles for the H.264 codec. Each profile defines a set of capabilities and constraints
        /// on which decoders rely.
        /// </summary>
        public enum H264EncodingProfile
        {
            /// <summary>
            /// Encode video with the baseline profile.
            /// </summary>
            [InspectorName("H.264 Baseline")] Baseline,
            /// <summary>
            /// Encode video using the main profile.
            /// </summary>
            [InspectorName("H.264 Main")] Main,
            /// <summary>
            /// Encode video with the high profile.
            /// </summary>
            [InspectorName("H.264 High")] High
        }

        /// <summary>
        /// The target bitrate, in Mbps, for the H.264 codec.
        /// </summary>
        public float TargetBitRate
        {
            get => targetBitRate;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException($"The target bitrate must be greater than zero.");
                if (value >= kMaxSupportedBitrate)
                    throw new ArgumentOutOfRangeException($"The target bitrate must be lower than {kMaxSupportedBitrate} Mbps.");
                targetBitRate = value;
            }
        }
        [SerializeField] internal float targetBitRate = 8f; // default 8 Mbps

        /// <summary>
        /// The target bitrate, in bps, for the H.264 codec.
        /// </summary>
        internal uint TargetBitRateBitsPerSecond => (uint)(TargetBitRate * 1000 * 1000); // Mbps to bps

        /// <summary>
        /// The interval in frames between two full images (I-frames), known as the Group of Pictures (GOP) size for the H.264 codec.
        /// </summary>
        public uint GopSize
        {
            get => gopSize;
            set => gopSize = value;
        }
        [SerializeField] internal uint gopSize = 25;

        /// <summary>
        /// The number of consecutive bidirectional predicted pictures (B-frames) for the H.264 codec.
        /// <remarks>
        /// The maximum supported value is 2.
        /// </remarks>
        /// </summary>
        public uint NumConsecutiveBFrames
        {
            get => numConsecutiveBFrames;
            set
            {
                if (value > 2)
                    throw new ArgumentOutOfRangeException($"The number of consecutive B-frames must not be greater than 2.");
                numConsecutiveBFrames = value;
            }
        }
        [SerializeField] internal uint numConsecutiveBFrames = 2;

        /// <summary>
        /// The choice of encoding profile for the H.264 codec. Each profile defines a set of capabilities and constraints
        /// on which decoders rely.
        /// </summary>
        public H264EncodingProfile EncodingProfile
        {
            get => encodingProfile;
            set => encodingProfile = value;
        }
        [SerializeField] internal H264EncodingProfile encodingProfile = H264EncodingProfile.High;

        /// <summary>
        /// The maximum interval in frames between two full images (I-frames), for the VP8 codec.
        /// </summary>
        public uint KeyframeDistance
        {
            get => keyframeDistance;
            set => keyframeDistance = value;
        }
        [SerializeField] internal uint keyframeDistance = 25;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CoreEncoderSettings()
        {
#if UNITY_EDITOR_LINUX
            Codec = OutputCodec.WEBM;
#else
            Codec = OutputCodec.MP4;
#endif
        }

        /// <summary>
        /// The selected codec of the encoder instance.
        /// </summary>
        public OutputCodec Codec
        {
            get => codec;
            set => codec = value;
        }

        /// <inheritdoc/>
        bool IEncoderSettings.CanCaptureAlpha => CodecSupportsTransparency(Codec);

        /// <inheritdoc/>
        bool IEncoderSettings.CanCaptureAudio => true;

        [SerializeField] OutputCodec codec;
        [SerializeField] VideoEncodingQuality encodingQuality = VideoEncodingQuality.High;

        /// <summary>
        /// Indicates the encoding quality to use for the encoding.
        /// </summary>
        public VideoEncodingQuality EncodingQuality
        {
            get { return encodingQuality; }
            set { encodingQuality = value; }
        }

        /// <inheritdoc/>
        string IEncoderSettings.Extension
        {
            get
            {
                if (codec == OutputCodec.MP4)
                    return "mp4";
                else
                    return "webm";
            }
        }

        /// <inheritdoc/>
        EncoderCoordinateConvention IEncoderSettings.CoordinateConvention => EncoderCoordinateConvention.OriginIsBottomLeft;

        /// <inheritdoc/>
        TextureFormat IEncoderSettings.GetTextureFormat(bool inputContainsAlpha)
        {
            return TextureFormat.RGBA32;
        }

        /// <inheritdoc/>
        void IEncoderSettings.ValidateRecording(RecordingContext ctx, List<string> errors, List<string> warnings)
        {
            if (!IsCodecSupportedOnThisPlatform(Codec))
                errors.Add($"Codec '{Codec}' is not supported on this platform.");

            if (Codec == OutputCodec.MP4)
            {
                if (ctx.height % 2 != 0 || ctx.width % 2 != 0)
                    errors.Add($"The MP4 format does not support odd values in resolution: {ctx.height}x{ctx.width}");

                if (ctx.height > kMaxSupportedSize_H264)
                    warnings.Add(
                        $"The image size exceeds the recommended maximum height for H.264: {(int)kMaxSupportedSize_H264} px");

                if (EncodingQuality == VideoEncodingQuality.Custom)
                {
                    if (NumConsecutiveBFrames > 2)
                        errors.Add($"The number of consecutive B-frames must not be greater than 2.");

                    if (TargetBitRate <= 0)
                        errors.Add($"The target bitrate must be greater than zero.");
                    if (TargetBitRate >= kMaxSupportedBitrate)
                        errors.Add($"The target bitrate must be lower than {kMaxSupportedBitrate} Mbps.");
                }
            }
            else if (Codec == OutputCodec.WEBM)
            {
                if (ctx.height > kMaxSupportedSize_VP8)
                    warnings.Add(
                        $"The image size exceeds the recommended maximum height for VP8: {(int)kMaxSupportedSize_VP8} px");
            }

            if (ctx.doCaptureAlpha && !CodecSupportsTransparency(Codec))
                errors.Add($"Codec '{Codec}' does not support transparency.");

            if (ctx.doCaptureAudio && !UnityHelpers.IsNumAudioChannelsSupported())
                errors.Add(UnityHelpers.GetUnsupportedSpeakerModeErrorMessage("Unity Media Encoder", kSupportedSpeakerModes));
        }

        internal bool CodecSupportsTransparency(OutputCodec outputCodec)
        {
            switch (outputCodec)
            {
                case OutputCodec.MP4:
                    return false;
                case OutputCodec.WEBM:
                    return true;
                default:
                    throw new InvalidEnumArgumentException($"Unexpected format '{outputCodec}'");
            }
        }

        /// <summary>
        /// Indicates whether the specified codec is supported or not on this platform.
        /// </summary>
        /// <param name="toCheck">The codec to check.</param>
        /// <returns>True if the codec is supported on this platform, False otherwise.</returns>
        internal static bool IsCodecSupportedOnThisPlatform(OutputCodec toCheck)
        {
#if UNITY_EDITOR_LINUX
            if (toCheck == OutputCodec.MP4)
                return false;
#endif
            return true;
        }

        /// <inheritdoc/>
        public bool SupportsCurrentPlatform()
        {
            return true;
        }

        /// <inheritdoc/>
        bool IEquatable<CoreEncoderSettings>.Equals(CoreEncoderSettings other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return codec == other.codec && encodingQuality == other.encodingQuality && targetBitRate == other.targetBitRate && gopSize == other.gopSize && numConsecutiveBFrames == other.numConsecutiveBFrames && encodingProfile == other.encodingProfile && keyframeDistance == other.keyframeDistance;
        }

        /// <summary>
        /// Compares the current object with another one.
        /// </summary>
        /// <param name="obj">The object to compare with the current one.</param>
        /// <returns>True if the two objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) ||
                obj is CoreEncoderSettings other && ((IEquatable<CoreEncoderSettings>) this).Equals(other);
        }

        /// <summary>
        /// Returns a hash code of all serialized fields.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine((int)codec, (int)encodingQuality);
        }
    }
}
