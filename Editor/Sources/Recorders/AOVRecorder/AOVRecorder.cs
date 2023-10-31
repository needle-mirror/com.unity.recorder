using System;
using System.IO;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor.Recorder.FileFormats;
using System.Linq;
using UnityEditor.Recorder.Input;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEditor.Recorder
{
    class AOVRecorder : BaseTextureRecorder<AOVRecorderSettings>
    {
        Queue<string> m_PathQueue = new();

        List<JobHandle> m_WriteFileHandles = new();
        List<WriteImageFrameJobBuffers> m_JobBuffers = new();

        ProfilerMarker m_ReadbackMarker = new("AOV Readback");
        ProfilerMarker m_PostProcessingMarker = new("AOV Data Post-Processing");

        protected override TextureFormat ReadbackTextureFormat
        {
            get
            {
                return Settings.m_OutputFormat != ImageRecorderSettings.ImageRecorderOutputFormat.EXR
                    ? TextureFormat.RGBA32
                    : TextureFormat.RGBAFloat;
            }
        }

        protected internal override bool BeginRecording(RecordingSession session)
        {
#if !HDRP_AVAILABLE
            // This can happen with an AOV Recorder Clip in a project after removing HDRP
            return Settings.IsInvalid();
#else
            if (!base.BeginRecording(session))
            {
                return false;
            }

            var aovRequestAPIInput = m_Inputs[0] as AOVCameraAOVRequestAPIInput;

            if (Settings.IsInvalid())
            {
                Debug.LogError($"The '{Settings.name}' AOV Recorder has errors and cannot record any data.");
                return false;
            }
            aovRequestAPIInput.waitForAsyncTasks += () =>
            {
                foreach (var handle in m_WriteFileHandles)
                {
                    handle.Complete();
                }
                m_WriteFileHandles.Clear();
            }; // This is to prevent the input from destroying the textures before the async finished;

            // Did the user request a vertically flipped image? This is not supported.
            var aovCameraInputSettings = Settings.InputsSettings.First() as AOVCameraInputSettings;
            if (aovCameraInputSettings != null && aovCameraInputSettings.FlipFinalOutput)
            {
                Debug.LogWarning(
                    $"The '{Settings.name}' AOV Recorder can't vertically flip the image as requested. This option is not supported in AOV recording context.");
            }

            Settings.FileNameGenerator.CreateDirectory(session);
            return true;
#endif
        }

        protected internal override void EndRecording(RecordingSession session)
        {
            if (m_WriteFileHandles.Count > 0)
            {
                // If job is already completed, nothing will happen
                foreach (var writeFileHandle in m_WriteFileHandles)
                    writeFileHandle.Complete();

                m_WriteFileHandles.Clear();
            }

            if (m_JobBuffers.Count > 0)
            {
                foreach (var jobBuffer in m_JobBuffers)
                    jobBuffer.Dispose();

                m_JobBuffers.Clear();
            }

            base.EndRecording(session);
        }

        protected internal override void RecordFrame(RecordingSession session)
        {
            if (Settings.IsInvalid())
                return;

            if (m_Inputs.Count != 1)
                throw new Exception("Unsupported number of sources");

            if (Settings.IsMultiPartEXR)
            {
                var path = Settings.FileNameGenerator.BuildAbsolutePath(session);
                m_PathQueue.Enqueue(path);
            }
            else
            {
                var fullSelection = Settings.GetAOVSelection();
                for (int i = 0; i < fullSelection.Length; i++)
                {
                    Settings.SetAOVSelection(fullSelection[i]);
                    m_PathQueue.Enqueue(Settings.FileNameGenerator.BuildAbsolutePath(session));
                }

                Settings.SetAOVSelection(fullSelection);
            }

            base.RecordFrame(session);
        }

        protected override void WriteFrame(Texture2D tex)
        {
            DequeueTimeStamp();
            byte[] bytes;

            Profiler.BeginSample("AOVRecorder.EncodeImage");
            try
            {
                switch (Settings.m_OutputFormat)
                {
                    case ImageRecorderSettings.ImageRecorderOutputFormat.EXR:
                        bytes = tex.EncodeToEXR();
                        WriteToFile(bytes);
                        break;
                    case ImageRecorderSettings.ImageRecorderOutputFormat.PNG:
                        bytes = tex.EncodeToPNG();
                        WriteToFile(bytes);
                        break;
                    default:
                        Profiler.EndSample();
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                Profiler.EndSample();
            }

            if (m_Inputs[0] is BaseRenderTextureInput ||
                Settings.m_OutputFormat != ImageRecorderSettings.ImageRecorderOutputFormat.JPEG)
                UnityHelpers.Destroy(tex);
        }

        internal override bool WriteGPUTextureFrame(RenderTexture _)
        {
#if HDRP_AVAILABLE
            foreach (var handle in m_WriteFileHandles)
                handle.Complete();

            m_WriteFileHandles.Clear(); // block previous frame

            var input = m_Inputs[0] as AOVCameraAOVRequestAPIInput; //Input as AOVCameraInputSettings;

            var formats = Settings.GetAOVSelection()
                .Select(x => AOVCameraAOVRequestAPIInput.AOVInfoLookUp[x].WorkingTextureFormat)
                .ToList();
            var needAlphas = Settings.GetAOVSelection()
                .Select(x => AOVCameraAOVRequestAPIInput.AOVInfoLookUp[x].NeedAlpha)
                .ToList();
            var convertToSRGB = Settings.OutputColorSpace == ImageRecorderSettings.ColorSpaceType.sRGB_sRGB;

            if (m_JobBuffers.Count == 0)
            {
                var layerAttributes = ImageWriterHelper.BuildAttributes(Settings);
                if (Settings.IsMultiPartEXR)
                {
                    m_JobBuffers.Add(new WriteImageFrameJobBuffers(input.AovTextures[0].rt.width,
                        input.AovTextures[0].rt.height, formats, needAlphas, layerAttributes));
                }
                else
                {
                    for (var i = 0; i < input.AovTextures.Length; i++)
                    {
                        m_JobBuffers.Add(new WriteImageFrameJobBuffers(input.AovTextures[i].rt.width,
                            input.AovTextures[i].rt.height, new[] { formats[i] }, new[] { needAlphas[i] },
                            new[] { layerAttributes[i] }));
                    }
                }
            }

            for (var i = 0; i < input.AovTextures.Length; i++)
            {
                RenderTexture tex;
                using (m_PostProcessingMarker.Auto())
                {
                    tex = PostProcessor.Convert(input.AovTextures[i], !needAlphas[i] && formats[i] == GraphicsFormat.R16G16B16A16_SFloat, convertToSRGB);
                    RenderTexture.ReleaseTemporary(tex);
                }

                using (m_ReadbackMarker.Auto())
                {
                    var bytes = Settings.IsMultiPartEXR ? m_JobBuffers[0].framesData[i] : m_JobBuffers[i].framesData[0];
                    AsyncGPUReadback.RequestIntoNativeArray(ref bytes, tex, 0,
                        formats[i]).WaitForCompletion();
                }
            }

            for (int i = 0; i < m_JobBuffers.Count; i++)
            {
                var writeImageJob = new WriteImageFrameJob
                {
                    FramesData = m_JobBuffers[i].framesData,
                    Width = (uint)input.AovTextures[i].rt.width,
                    Height = (uint)input.AovTextures[i].rt.height,
                    FileAttributes = m_JobBuffers[i].fileAttributes,
                    FilePath = m_PathQueue.Dequeue()
                };

                m_WriteFileHandles.Add(writeImageJob.Schedule());
            }

            return true;
#else
            return false;
#endif
        }

        private void WriteToFile(byte[] bytes)
        {
            Profiler.BeginSample("AOVRecorder.WriteToFile");
            File.WriteAllBytes(m_PathQueue.Dequeue(), bytes);
            Profiler.EndSample();
        }
    }
}
