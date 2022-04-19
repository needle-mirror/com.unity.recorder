#if HDRP_AVAILABLE
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Recorder
{
    static class HdrpHelper
    {
        static class Messages
        {
            public static readonly string ColorBuffer = $"The Color Buffer Format does not support alpha, you can change this in the \"Rendering\" section of your \"{nameof(HDRenderPipelineAsset)}\"";
            public static readonly string CustomPassBuffer = $"The Custom Pass Buffer Format does not support alpha, you can change this in the \"Rendering\" section of your \"{nameof(HDRenderPipelineAsset)}\"";
            public static readonly string PostProcessingBuffer = $"The Post Processing Buffer Format does not support alpha, you can change this in the \"Post-processing\" section of your \"{nameof(HDRenderPipelineAsset)}\"";
        }

        public static void CheckRenderPipelineAssetAlphaSupport(List<string> warnings)
        {
            if (GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset hdRenderPipelineAsset)
            {
                var renderPipelineSettings = hdRenderPipelineAsset.currentPlatformRenderPipelineSettings;

                if (!HasAlpha(renderPipelineSettings.colorBufferFormat))
                {
                    warnings.Add(Messages.ColorBuffer);
                }

                if (!HasAlpha(renderPipelineSettings.customBufferFormat))
                {
                    warnings.Add(Messages.CustomPassBuffer);
                }

                if (!HasAlpha(renderPipelineSettings.postProcessSettings.bufferFormat))
                {
                    warnings.Add(Messages.PostProcessingBuffer);
                }
            }
        }

        static bool HasAlpha(RenderPipelineSettings.ColorBufferFormat format)
        {
            switch (format)
            {
                case RenderPipelineSettings.ColorBufferFormat.R11G11B10: return false;
                case RenderPipelineSettings.ColorBufferFormat.R16G16B16A16: return true;
            }

            return false;
        }

        static bool HasAlpha(RenderPipelineSettings.CustomBufferFormat format)
        {
            switch (format)
            {
                case RenderPipelineSettings.CustomBufferFormat.R11G11B10: return false;
                case RenderPipelineSettings.CustomBufferFormat.R8G8B8A8: return true;
                case RenderPipelineSettings.CustomBufferFormat.R16G16B16A16: return true;
                case RenderPipelineSettings.CustomBufferFormat.SignedR8G8B8A8: return true;
            }

            return false;
        }

        static bool HasAlpha(PostProcessBufferFormat format)
        {
            switch (format)
            {
                case PostProcessBufferFormat.R11G11B10: return false;
                case PostProcessBufferFormat.R16G16B16A16: return true;
                case PostProcessBufferFormat.R32G32B32A32: return true;
            }

            return false;
        }
    }
}
#endif
