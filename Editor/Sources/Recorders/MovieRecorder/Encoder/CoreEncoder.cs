using System.ComponentModel;
using Unity.Collections;
using UnityEditor.Media;
using UnityEngine;

namespace UnityEditor.Recorder.Encoder
{
    /// <summary>
    /// The Core Media Encoder
    /// </summary>
    class CoreEncoder : IEncoder
    {
        private MediaEncoder encoderHandle;
        private VideoTrackAttributes videoAttributes; // for the old API
        private VideoTrackEncoderAttributes videoEncoderAttributes; // for the advanced API
        private AudioTrackAttributes audioAttributes;
        private bool disposed = false;
        private bool usingNewAPI; // whether or not we are using the new API (advanced encoding options)

        public void OpenStream(IEncoderSettings settings, RecordingContext ctx)
        {
            var coreSettings = settings as CoreEncoderSettings;

            // Create the file
            usingNewAPI = coreSettings.EncodingQuality == CoreEncoderSettings.VideoEncodingQuality.Custom;

            if (usingNewAPI)
            {
                // Use the API with VideoTrackEncoderAttributes: populate videoEncoderAttributes
                switch (coreSettings.Codec)
                {
                    case CoreEncoderSettings.OutputCodec.MP4:
                        // Set up advanced H.264 options
                        VideoEncodingProfile vep = (int)coreSettings.EncodingProfile < 1
                            ? VideoEncodingProfile.H264Main
                            : VideoEncodingProfile.H264High;

                        if (coreSettings.EncodingProfile == (int)CoreEncoderSettings.H264EncodingProfile.Baseline)
                        {
                            vep = VideoEncodingProfile.H264Baseline;
                        }

                        var h264Attr = new H264EncoderAttributes
                        {
                            gopSize = coreSettings.GopSize,
                            numConsecutiveBFrames = coreSettings.NumConsecutiveBFrames,
                            profile = vep
                        };

                        videoEncoderAttributes = new VideoTrackEncoderAttributes(h264Attr)
                        {
                            frameRate = ctx.fps,
                            width = (uint)ctx.width,
                            height = (uint)ctx.height,
                            includeAlpha = ctx.doCaptureAlpha,
                            bitRateMode = VideoBitrateMode.High, // so that audio encoder uses high bitrate
                            targetBitRate = coreSettings.TargetBitRateBitsPerSecond  // H.264 expects bps
                        };
                        break;
                    case CoreEncoderSettings.OutputCodec.WEBM:
                        // Set up advanced VP8 options
                        var vp8Attr = new VP8EncoderAttributes()
                        {
                            keyframeDistance = coreSettings.keyframeDistance
                        };

                        videoEncoderAttributes = new VideoTrackEncoderAttributes(vp8Attr)
                        {
                            frameRate = ctx.fps,
                            width = (uint)ctx.width,
                            height = (uint)ctx.height,
                            includeAlpha = ctx.doCaptureAlpha,
                            bitRateMode = VideoBitrateMode.High, // so that audio encoder uses high bitrate
                            targetBitRate = coreSettings.TargetBitRateBitsPerSecond // VP8 expects bps
                        };
                        break;
                    default:
                        throw new InvalidEnumArgumentException($"Unsupported codec '{coreSettings.Codec}'");
                }
            }
            else
            {
                // Use the old API: populate videoAttributes
                videoAttributes = new VideoTrackAttributes
                {
                    frameRate = ctx.fps,
                    width = (uint)ctx.width,
                    height = (uint)ctx.height,
                    includeAlpha = ctx.doCaptureAlpha,
                    bitRateMode = EncodingQualityToBitrateMode(coreSettings.EncodingQuality)
                };
            }

            if (ctx.doCaptureAudio)
            {
                audioAttributes = new AudioTrackAttributes()
                {
                    channelCount = (ushort)UnityHelpers.GetNumAudioChannels(),
                    sampleRate = new MediaRational(AudioSettings.outputSampleRate),
                    language = ""
                };
                if (usingNewAPI)
                    encoderHandle = new MediaEncoder(ctx.path, videoEncoderAttributes, audioAttributes);
                else
                    encoderHandle = new MediaEncoder(ctx.path, videoAttributes, audioAttributes);
            }
            else
            {
                // No audio
                if (usingNewAPI)
                    encoderHandle = new MediaEncoder(ctx.path, videoEncoderAttributes);
                else
                    encoderHandle = new MediaEncoder(ctx.path, videoAttributes);
            }
            disposed = false;
        }

        public void CloseStream()
        {
            if (encoderHandle != null)
                encoderHandle.Dispose(); // Error will have been triggered earlier
            disposed = true;
        }

        public void AddVideoFrame(NativeArray<byte> bytes, MediaTime time)
        {
            if (disposed)
            {
                Debug.LogError($"The encoder has already been disposed, ignoring this data.");
                return;
            }
            var success = encoderHandle.AddFrame(GetWidth(), GetHeight(), 0, TextureFormat.RGBA32, bytes, time);
            if (!success)
                Debug.LogError("Failed to add video frame to Unity Media Encoder encoder");
        }

        public void AddAudioFrame(NativeArray<float> bytes)
        {
            if (disposed)
            {
                Debug.LogError($"The encoder has already been disposed, ignoring this data.");
                return;
            }

            if (bytes.Length == 0)
            {
                return;
            }

            var success = encoderHandle.AddSamples(bytes);
            if (!success)
                Debug.LogError("Failed to add audio samples to Unity Media Encoder");
        }

        public static VideoBitrateMode EncodingQualityToBitrateMode(CoreEncoderSettings.VideoEncodingQuality quality)
        {
            switch (quality)
            {
                case CoreEncoderSettings.VideoEncodingQuality.Low:
                    return VideoBitrateMode.Low;
                case CoreEncoderSettings.VideoEncodingQuality.Medium:
                    return VideoBitrateMode.Medium;
                case CoreEncoderSettings.VideoEncodingQuality.High:
                    return VideoBitrateMode.High;
                default:
                    throw new InvalidEnumArgumentException($"Unexpected enum value '{quality}'");
            }
        }

        int GetWidth()
        {
            if (usingNewAPI)
                return (int)videoEncoderAttributes.width;
            else
                return (int)videoAttributes.width;
        }

        int GetHeight()
        {
            if (usingNewAPI)
                return (int)videoEncoderAttributes.height;
            else
                return (int)videoAttributes.height;
        }
    }
}
