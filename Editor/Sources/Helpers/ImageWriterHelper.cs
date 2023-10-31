using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor.Bindings.OpenImageIO;

namespace UnityEditor.Recorder
{
    static class ImageWriterHelper
    {
        internal static List<List<OiioWrapper.Attribute>> BuildAttributes(AOVRecorderSettings settings)
        {
            // call the Beauty rgba so it maps nicely in Nuke
            var layerNames = settings.GetAOVSelection()
                .Select(x => new FixedString4096Bytes(x == AOVType.Beauty ? "rgba" : x.ToString())).ToList();

            var allSubImagesAttributes = new List<List<OiioWrapper.Attribute>>();

            var compression = settings.EXRCompression.ToString().ToLower();
            if (CompressionUtility.SupportsCompressionLevel(settings.EXRCompression))
            {
                compression += $":{settings.EXRCompressionLevel}";
            }

            for (int i = 0; i < layerNames.Count; i++)
            {
                var attributes = new List<OiioWrapper.Attribute>
                {
                    new()
                    {
                        key = "oiio:subimagename",
                        value = layerNames[i]
                    },
                    new()
                    {
                        key = "compression",
                        value = compression
                    },
                    new() {
                        key = "oiio:ColorSpace",
                        value = settings.OutputColorSpace == ImageRecorderSettings.ColorSpaceType.Unclamped_linear_sRGB ? "scene_linear" : "sRGB"
                    }
                };

                allSubImagesAttributes.Add(attributes);
            }

            return allSubImagesAttributes;
        }
    }
}
