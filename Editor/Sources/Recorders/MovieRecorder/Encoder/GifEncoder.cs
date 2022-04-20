using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Recorder.Encoder
{
    class GifEncoder : IEncoder, IDisposable
    {
        private IntPtr encoderPtr;
        private JobHandle addVideoFrameHandle;
        private NativeArray<byte> returnValue;
        private NativeArray<byte> pixels;

        public void OpenStream(IEncoderSettings settings, RecordingContext ctx)
        {
            var gifSettings = settings as GifEncoderSettings;
            bool constantFrameRate = ctx.frameRateMode == FrameRatePlayback.Constant;
            var fps = (float)MovieRecorder.DoubleFromRational(ctx.fps);
            encoderPtr = GIFWrapper.Create(ctx.path, ctx.width, ctx.height, gifSettings.Loop,  constantFrameRate, true, fps, (int)gifSettings.Quality);
            if (encoderPtr == IntPtr.Zero)
                Debug.LogError($"Could not create file {ctx.path}");

            returnValue = new NativeArray<byte>(new[] {byte.MaxValue}, Allocator.Persistent);
        }

        public void CloseStream()
        {
            (this as IDisposable).Dispose();
            if (encoderPtr == IntPtr.Zero)
                return; // Error will have been triggered earlier
            bool success = GIFWrapper.Close(encoderPtr);
            if (!success)
                Debug.LogError("Failed to close GIF encoder");
            encoderPtr = new IntPtr(); // This protects against a double free.
        }

        public unsafe void AddVideoFrame(NativeArray<byte> bytes, MediaTime time)
        {
            addVideoFrameHandle.Complete();

            if (encoderPtr == IntPtr.Zero)
                return; // Error will have been triggered earlier

            // this reports errors that occured previous frame.
            if (!Convert.ToBoolean(returnValue[0]))
            {
                Debug.LogError("Failed to add video frame to ProRes encoder");
            }

            if (!pixels.IsCreated)
            {
                pixels = new NativeArray<byte>(bytes.Length, Allocator.Persistent);
            }
            else if (pixels.Length != bytes.Length)
            {
                pixels.Dispose();
                pixels = new NativeArray<byte>(bytes.Length, Allocator.Persistent);
            }
            pixels.CopyFrom(bytes);

            var addVideoFrameJob = new AddVideoFrameJob
            {
                encoderPtr = encoderPtr,
                bytes = pixels.GetUnsafeReadOnlyPtr(),
                time = time,
                result = returnValue
            };
            addVideoFrameHandle = addVideoFrameJob.Schedule();
        }

        public void AddAudioFrame(NativeArray<float> bytes)
        {
            Debug.LogError("The GIF encoder does not accept audio samples.");
        }

        void IDisposable.Dispose()
        {
            addVideoFrameHandle.Complete();
            if (returnValue.IsCreated)
            {
                returnValue.Dispose();
            }

            if (pixels.IsCreated)
            {
                pixels.Dispose();
            }
        }

        unsafe struct AddVideoFrameJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public IntPtr encoderPtr;
            [NativeDisableUnsafePtrRestriction] public void* bytes;
            public MediaTime time;

            public NativeArray<byte> result;

            public void Execute()
            {
                float seconds = (float)(time.count / (time.rate.numerator * (double)time.rate.denominator));
                float ms = seconds * 1000.0f;
                var res = GIFWrapper.AddVideoFrame(encoderPtr, bytes, ms);
                result[0] = Convert.ToByte(res);
            }
        }
    }
}
