using System;
using System.Linq;
using Unity.Collections;
using UnityEditor.Media;
using UnityEngine;

namespace UnityEditor.Recorder.Encoder
{
    class ProResEncoder : IEncoder
    {
        private IntPtr encoderPtr;

        public void OpenStream(IEncoderSettings settings, RecordingContext ctx)
        {
            var proResSettings = settings as ProResEncoderSettings;
#if UNITY_EDITOR_OSX
            // Ensure that this codec format is supported, because on macOS we depend on AVFoundation in the OS
            System.Text.StringBuilder sb = new System.Text.StringBuilder(128);
            bool supported = ProResWrapperHelpers.SupportsCodecFormat((int)proResSettings.Format, sb, sb.Capacity);

            if (!supported)
            {
                Debug.LogError($"Could not create file {ctx.path}: {sb}");
                encoderPtr = IntPtr.Zero;
                return;
            }
#endif

            // If the file has audio, it will always be stereo
            var audioSampleRate = new MediaRational(AudioSettings.outputSampleRate);
            var colorDefinition = (int)ProResEncoderSettings.ProResColorDefinition.HD_Rec709; // hardcoded for now

            encoderPtr = ProResWrapper.Create(UnityHelpers.PackageDescription, ctx.path, ctx.width, ctx.height, (float)MovieRecorder.DoubleFromRational(ctx.fps), ctx.doCaptureAudio, (float)MovieRecorder.DoubleFromRational(audioSampleRate), (int)proResSettings.Format, ctx.doCaptureAlpha, colorDefinition);
            if (encoderPtr == IntPtr.Zero)
                Debug.LogError($"Could not create file {ctx.path}");
        }

        public void CloseStream()
        {
            if (encoderPtr == IntPtr.Zero)
                return; // Error will have been triggered earlier
            bool success = false;
            success = ProResWrapper.Close(encoderPtr);
            if (!success)
                Debug.LogError("Failed to close ProRes encoder");
        }

        public void AddVideoFrame(NativeArray<byte> bytes, MediaTime time)
        {
            if (encoderPtr == IntPtr.Zero)
                return; // Error will have been triggered earlier

            bool success = ProResWrapper.AddVideoFrame(encoderPtr, bytes.ToArray(), time.count, time.rate.numerator, time.rate.denominator);
            if (!success)
                Debug.LogError("Failed to add video frame to ProRes encoder");
        }

        public void AddAudioFrame(NativeArray<float> bytes)
        {
            if (encoderPtr == IntPtr.Zero)
                return; // Error will have been triggered earlier

            // Recorder clips may send empty buffers, in which case success will still be true.
            bool success = ProResWrapper.AddAudioSamples(encoderPtr, bytes.ToArray(), bytes.Count());
            if (!success)
                Debug.LogError("Failed to add audio samples to ProRes encoder");
        }
    }
}
