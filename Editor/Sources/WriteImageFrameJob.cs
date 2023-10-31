using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Experimental.Rendering;
using UnityEditor.Bindings.OpenImageIO;

namespace UnityEditor.Recorder.FileFormats
{
    /// <summary>
    /// Job to write and image frame
    /// </summary>
    unsafe struct WriteImageFrameJob : IJob
    {
        /// <summary>
        /// List of byte arrays with frame data.
        /// </summary>
        public UnsafeList<NativeArray<byte>> FramesData;

        /// <summary>
        /// Frame width.
        /// </summary>
        public uint Width;

        /// <summary>
        /// Frame height.
        /// </summary>
        public uint Height;

        /// <summary>
        /// List of file attributes arrays.
        /// </summary>
        public UnsafeList<NativeArray<OiioWrapper.Attribute>> FileAttributes;

        /// <summary>
        /// Path to file.
        /// </summary>
        public FixedString4096Bytes FilePath;

        /// <summary>
        /// Execute the job.
        /// </summary>
        public void Execute()
        {
            WriteOiioImageFrames(FramesData, Width, Height,
                FileAttributes, FilePath);
        }

        static void WriteOiioImageFrames(UnsafeList<NativeArray<byte>> frames, uint width, uint height,
            UnsafeList<NativeArray<OiioWrapper.Attribute>> fileAttributes, FixedString4096Bytes path)
        {
            var headers = new NativeArray<OiioWrapper.ImageHeader>(frames.Length, Allocator.Temp);

            for (var i = 0; i < frames.Length; i++)
            {
                const int sizeHalf = 2;

                var channelsCount = (uint)(frames[i].Length / (width * height * sizeHalf));

                headers[i] = new OiioWrapper.ImageHeader
                {
                    width = width,
                    height = height,
                    channelsCount = channelsCount,
                    data = new IntPtr(frames[i].GetUnsafeReadOnlyPtr()),
                    attributesCount = (uint)fileAttributes[i].Length,
                    attributes = new IntPtr(fileAttributes[i].GetUnsafeReadOnlyPtr())
                };
            }

            OiioWrapper.WriteImage(path, (uint)frames.Length,
                (OiioWrapper.ImageHeader*)headers.GetUnsafeReadOnlyPtr());
        }
    }

    /// <summary>
    /// Class to write image frame job buffers
    /// </summary>
    class WriteImageFrameJobBuffers : IDisposable
    {
        /// <summary>
        /// Byte array of image data
        /// </summary>
        public UnsafeList<NativeArray<byte>> framesData;

        /// <summary>
        /// Attributes of file
        /// </summary>
        public UnsafeList<NativeArray<OiioWrapper.Attribute>> fileAttributes;

        /// <summary>
        /// Write Frame Job Buffers for an array of images
        /// </summary>
        /// <param name="width">Frame width</param>
        /// <param name="height">Frame height</param>
        /// <param name="readbackFormats">List of readback formats</param>
        /// <param name="needAlphas">List of booleans to indicate if the layer needs alpha</param>
        /// <param name="layersAttributesList">List of list of attributes</param>
        public WriteImageFrameJobBuffers(int width, int height, IList<GraphicsFormat> readbackFormats, IList<bool> needAlphas,
                                         IList<List<OiioWrapper.Attribute>> layersAttributesList)
        {
            framesData = new UnsafeList<NativeArray<byte>>(0, Allocator.Persistent);
            fileAttributes = new UnsafeList<NativeArray<OiioWrapper.Attribute>>(0, Allocator.Persistent);

            for (int i = 0; i < layersAttributesList.Count; ++i)
            {
                var bufferSize = ComputeBufferSize(width, height, readbackFormats[i], needAlphas[i]);
                framesData.Add(new NativeArray<byte>(bufferSize, Allocator.Persistent));

                var layerAttributes =
                    new NativeArray<OiioWrapper.Attribute>(layersAttributesList[i].Count, Allocator.Persistent);
                layerAttributes.CopyFrom(layersAttributesList[i].ToArray());
                fileAttributes.Add(layerAttributes);
            }
        }

        static int ComputeBufferSize(int width, int height, GraphicsFormat format, bool needAlpha)
        {
            var mipmapSize = (int)GraphicsFormatUtility.ComputeMipmapSize(width, height, format);

            if (format is GraphicsFormat.R16G16B16A16_SFloat && !needAlpha)
            {
                return mipmapSize * 3 / 4;
            }

            return mipmapSize;
        }

        /// <summary>
        /// Disposes of the frame.
        /// </summary>
        public void Dispose()
        {
            if (framesData.IsCreated)
            {
                foreach (var frame in framesData)
                {
                    if (frame.IsCreated)
                    {
                        frame.Dispose();
                    }
                }

                framesData.Dispose();
            }

            if (fileAttributes.IsCreated)
            {
                foreach (var attrib in fileAttributes)
                {
                    if (attrib.IsCreated)
                    {
                        attrib.Dispose();
                    }
                }

                fileAttributes.Dispose();
            }
        }
    }
}
