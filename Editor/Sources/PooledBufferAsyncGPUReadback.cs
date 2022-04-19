using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEditor.Recorder
{
    sealed class PooledBufferAsyncGPUReadback : IDisposable
    {
        List<Tuple<AsyncGPUReadbackRequest, NativeArray<byte>>> asyncBuffers = new();
        Dictionary<NativeArray<byte>, JobHandle> bufferJobLocks = new();

        public AsyncGPUReadbackRequest RequestGPUReadBack(RenderTexture tex, GraphicsFormat format, Action<AsyncGPUReadbackRequest> cb)
        {
            var buff = new NativeArray<byte>();
            GetAsyncBuffer(tex.width, tex.height, format, ref buff); // Gets a buffer from the bufferpool
            var req = AsyncGPUReadback.RequestIntoNativeArray(ref buff, tex, 0, format, cb);
            RegisterAsyncBuffer(req,
                ref buff); // Associates the buffer with an asyncRequest to make sure it is used only when free.

            return req;
        }

        public AsyncGPUReadbackRequest RequestGPUReadBack(RenderTexture tex, Action<AsyncGPUReadbackRequest> cb)
        {
            return RequestGPUReadBack(tex, tex.graphicsFormat, cb);
        }

        public void RegisterJobDependency(ref NativeArray<byte> buffer, JobHandle handle)
        {
            if (!bufferJobLocks.ContainsKey(buffer))
            {
                Debug.LogError("Buffer is not managed by this PooledBufferAsyncGPUReadback");
                return;
            }

            bufferJobLocks[buffer] = handle;
        }

        void GetAsyncBuffer(int width, int height, GraphicsFormat format, ref NativeArray<byte> buff)
        {
            NativeArray<byte> ret = default;
            var sz = (int)UnityEngine.Experimental.Rendering.GraphicsFormatUtility.ComputeMipmapSize(width,
                height,
                format); // Might not be able to use it.

            int idx;
            var found = false;
            for (idx = 0; idx < asyncBuffers.Count; ++idx)
            {
                if (asyncBuffers[idx].Item1.done && asyncBuffers[idx].Item2.Length == sz && bufferJobLocks[asyncBuffers[idx].Item2].IsCompleted)
                {
                    ret = asyncBuffers[idx].Item2;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                ret = new NativeArray<byte>(sz, Allocator.Persistent);
                asyncBuffers.Add(
                    new Tuple<AsyncGPUReadbackRequest, NativeArray<byte>>(default,
                        ret)); // Register the buffer with a dummy request
                bufferJobLocks[ret] = default;
            }

            buff = ret;
        }

        void RegisterAsyncBuffer(AsyncGPUReadbackRequest r, ref NativeArray<byte> buff)
        {
            for (var idx = 0; idx < asyncBuffers.Count; ++idx)
            {
                if (asyncBuffers[idx].Item2 == buff)
                {
                    asyncBuffers[idx] = new Tuple<AsyncGPUReadbackRequest, NativeArray<byte>>(r, buff);
                    return;
                }
            }

            throw new InvalidOperationException("The buffer is not registered to the Recorder buffer pool");
        }

        public void Dispose()
        {
            foreach (var buffer in asyncBuffers)
            {
                buffer.Item1.WaitForCompletion();
            }

            foreach (var value in bufferJobLocks.Values)
            {
                value.Complete();
            }

            foreach (var asyncBuffer in asyncBuffers)
            {
                asyncBuffer.Item2.Dispose();
            }

            asyncBuffers.Clear();
            bufferJobLocks.Clear();
        }
    }
}
