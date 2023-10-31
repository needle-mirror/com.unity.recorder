using UnityEngine;

namespace UnityEditor.Recorder
{
    // This class is used to apply some post processing on images, first, flipping the texture on its Y axis
    // second, dropping the alpha channel for AOVs that have only 3 channels (Normal, Albedo, etc.) and
    // third, converting to the correct colorspace if needed
    static class PostProcessor
    {
        static Material mat;

        // The user needs to deallocate the resulting RT
        public static RenderTexture Convert(RenderTexture input, bool dropAlpha, bool shouldConvertToSRGB, bool flipY = true)
        {
            if (mat == null)
            {
                mat = new Material(Shader.Find("Hidden/Recorder/PostProcessor"));
            }
            else
            {
                mat.DisableKeyword("CONVERT_TO_SRGB");
                mat.DisableKeyword("DROP_ALPHA");
                mat.DisableKeyword("FLIP_Y");
            }

            int outWidth = input.width;
            if (shouldConvertToSRGB)
            {
                mat.EnableKeyword("CONVERT_TO_SRGB");
            }

            if (dropAlpha)
            {
                mat.EnableKeyword("DROP_ALPHA");
                outWidth = (outWidth * 3) / 4;
            }

            if (flipY)
            {
                mat.EnableKeyword("FLIP_Y");
            }


            // Assume half
            input.filterMode = FilterMode.Point;
            var ret = RenderTexture.GetTemporary(outWidth, input.height, 0, input.graphicsFormat);
            using (new RenderTextureActiveGuard(RenderTexture.active))
            {
                Graphics.Blit(input, ret, mat, 0);
            }

            ret.name = "PostProcessor Result";
            return ret;
        }
    }
}
