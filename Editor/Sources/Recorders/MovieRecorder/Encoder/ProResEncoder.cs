using System;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEditor.Recorder.Encoder
{
    class ProResEncoder : IEncoder, IDisposable
    {
        private IntPtr encoderPtr;

        private RecordingContext context;
        private ProResEncoderSettings.OutputFormat proResFormat;

        private PooledBufferAsyncGPUReadback asyncReadback;
        private JobHandle addVideoFrameHandle;
        private NativeArray<byte> returnValue;
        private Material mat;

        private ProfilerMarker postProcessMarker = new ProfilerMarker("ProRes PostProcess Pixel Data");
        private ProfilerMarker addVideoFrameMarker = new ProfilerMarker("ProResEncoder.AddVideoFrame");

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
            context = ctx;
            proResFormat = proResSettings.Format;

            encoderPtr = ProResWrapper.Create(UnityHelpers.PackageDescription, ctx.path, ctx.width, ctx.height, (float)MovieRecorder.DoubleFromRational(ctx.fps), ctx.doCaptureAudio, (float)MovieRecorder.DoubleFromRational(audioSampleRate), (int)proResSettings.Format, ctx.doCaptureAlpha, colorDefinition);
            if (encoderPtr == IntPtr.Zero)
                Debug.LogError($"Could not create file {ctx.path}");

            returnValue = new NativeArray<byte>(new[] {byte.MaxValue}, Allocator.Persistent);
            asyncReadback = new PooledBufferAsyncGPUReadback();
        }

        public void CloseStream()
        {
            (this as IDisposable).Dispose();
            if (encoderPtr == IntPtr.Zero)
                return; // Error will have been triggered earlier
            bool success = ProResWrapper.Close(encoderPtr);
            if (!success)
                Debug.LogError("Failed to close ProRes encoder");
            encoderPtr = new IntPtr(); // This protects against a double free.
        }

        private void GetARGBBytes(RenderTexture tex, Action<AsyncGPUReadbackRequest> cb)
        {
            // 64 bytes for output as ARGB64
            // Input bytes are in TextureFormat.RGBA64 format, where each pixel takes 8 bytes
            // Output bytes will be ARGB64, where each pixel takes 8 bytes, therefore the texture dimensions will be identical
            ApplyShader(tex, context.width, context.height, GraphicsFormat.R16G16B16A16_UNorm, TextureFormat.RGBA64, cb);
        }

        private void GetPackedYUVBytes(RenderTexture tex, Action<AsyncGPUReadbackRequest> cb)
        {
            // Convert from RGB24 to 2vuy 4:2:2 Y'CbCr 8-bit
            // Input bytes are in TextureFormat.RGB24 format, where each pixel takes 3 bytes
            // Output bytes will be 4 bytes (GraphicsFormat.R8G8B8A8_UNorm), where 4 bytes will store 2 pixels
            // Each output row will require half the number of "pixels" as the input
            var outputBytesPerRow = context.width * (1 + 0.5 + 0.5); // after packing, each incoming pixel will require in bytes: 1 per Y component + 0.5 for Cb + 0.5 for Cr
            var outputBytesPerPixel = 4;
            int outputRowSize = (int)(outputBytesPerRow / outputBytesPerPixel); // (remember output is 4 bytes per pixel vs 3 in input) gives context.width / 2
            ApplyShader(tex, outputRowSize, context.height, GraphicsFormat.R8G8B8A8_UNorm, TextureFormat.RGB24, cb);
        }

        private void ApplyShader(RenderTexture tex, int outputTextureWidth, int outputTextureHeight,
            GraphicsFormat asyncRequestedFormat, TextureFormat textureFormat, Action<AsyncGPUReadbackRequest> cb)
        {
            RenderTexture temporaryTextureFrame = RenderTexture.GetTemporary(outputTextureWidth,
                outputTextureHeight, 0, GraphicsFormat.R8G8B8A8_UNorm);
            temporaryTextureFrame.filterMode = FilterMode.Point;
            if (mat == null)
            {
                mat = new Material(Shader.Find("Hidden/Recorder/ProResPixelOptimizer"));

                if (tex.sRGB)
                {
                    mat.EnableKeyword("INPUT_IS_SRGB");
                }

                switch (textureFormat)
                {
                    case TextureFormat.RGBA64:
                        mat.EnableKeyword("RGBA64_TO_AYCBCR");
                        break;
                    case TextureFormat.RGB24:
                        mat.EnableKeyword("RGB24_TO_2VUY8BITS");
                        break;
                    default:
                        Debug.LogError(
                            $"Unexpected texture format '{textureFormat}' for the ProRes pixel format optimization shader.");
                        break;
                }
            }

            using (postProcessMarker.Auto())
            {
                var tmp = tex.filterMode;
                tex.filterMode = FilterMode.Point;
                var rt = RenderTexture.active;
                Graphics.Blit(tex, temporaryTextureFrame, mat, 0);
                RenderTexture.active = rt;
                tex.filterMode = tmp;
            }

            asyncReadback.RequestGPUReadBack(temporaryTextureFrame, asyncRequestedFormat, cb);
            RenderTexture.ReleaseTemporary(temporaryTextureFrame);
        }

        public IEncoder.VideoInputPath GetVideoInputPath => IEncoder.VideoInputPath.GPUBuffer;

        public void AddVideoFrame(RenderTexture tex, MediaTime time)
        {
            if (proResFormat == ProResEncoderSettings.OutputFormat.ProRes4444XQ ||
                proResFormat == ProResEncoderSettings.OutputFormat.ProRes4444)
            {
                GetARGBBytes(tex, request => { AddVideoFrame(request.GetData<byte>(), time); });
            }
            else
            {
                GetPackedYUVBytes(tex, request => { AddVideoFrame(request.GetData<byte>(), time); });
            }
        }

        public unsafe void AddVideoFrame(NativeArray<byte> bytes, MediaTime time)
        {
            using (addVideoFrameMarker.Auto())
            {
                addVideoFrameHandle.Complete();

                if (encoderPtr == IntPtr.Zero)
                    return; // Error will have been triggered earlier

                // this reports errors that occured previous frame.
                if (!Convert.ToBoolean(returnValue[0]))
                {
                    Debug.LogError("Failed to add video frame to ProRes encoder");
                }

                var addVideoFrameJob = new AddVideoFrameJob
                {
                    encoderPtr = encoderPtr,
                    bytes = bytes.GetUnsafeReadOnlyPtr(),
                    time = time,
                    result = returnValue
                };
                addVideoFrameHandle = addVideoFrameJob.Schedule();
                asyncReadback.RegisterJobDependency(ref bytes, addVideoFrameHandle);
            }
        }

        public void AddAudioFrame(NativeArray<float> bytes)
        {
            addVideoFrameHandle.Complete();
            if (encoderPtr == IntPtr.Zero)
                return; // Error will have been triggered earlier

            unsafe
            {
                // Recorder clips may send empty buffers, in which case success will still be true.
                var success =
                    ProResWrapper.AddAudioSamples(encoderPtr, (float*)bytes.GetUnsafeReadOnlyPtr(), bytes.Count());

                if (!success)
                    Debug.LogError("Failed to add audio samples to ProRes encoder");
            }
        }

        void IDisposable.Dispose()
        {
            addVideoFrameHandle.Complete();
            if (asyncReadback != null)
            {
                asyncReadback.Dispose();
                asyncReadback = null;
            }

            if (returnValue.IsCreated)
            {
                returnValue.Dispose();
            }

            mat = null;
        }

        unsafe struct AddVideoFrameJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public IntPtr encoderPtr;
            [NativeDisableUnsafePtrRestriction] public void* bytes;
            public MediaTime time;

            public NativeArray<byte> result;

            public void Execute()
            {
                var res = ProResWrapper.AddVideoFrame(encoderPtr, bytes, time.count, time.rate.numerator,
                    time.rate.denominator);
                result[0] = Convert.ToByte(res);
            }
        }
    }
}
