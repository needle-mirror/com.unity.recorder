using System.ComponentModel;
using UnityEditor.Recorder.AOV;
using UnityEngine;

namespace UnityEditor.Recorder
{
    static class EXRCompressionTypeExtensions
    {
        internal static Texture2D.EXRFlags ToNativeType(this EXRCompressionType type)
        {
            Texture2D.EXRFlags nativeType = Texture2D.EXRFlags.None;
            switch (type)
            {
                case EXRCompressionType.RLE:
                    nativeType = Texture2D.EXRFlags.CompressRLE;
                    break;
                case EXRCompressionType.Zip:
                    nativeType = Texture2D.EXRFlags.CompressZIP;
                    break;
                case EXRCompressionType.None:
                    nativeType = Texture2D.EXRFlags.None;
                    break;
                default:
                    throw new InvalidEnumArgumentException($"Unexpected compression type '{type}'.");
            }
            return nativeType;
        }
    }
}
