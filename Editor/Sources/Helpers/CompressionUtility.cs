using System.ComponentModel;
using UnityEngine;

namespace UnityEditor.Recorder
{
    /// <summary>
    /// Recorder Utility classes.
    /// </summary>
    public static class CompressionUtility
    {
        /// <summary>
        /// Compression type for EXR files.
        /// </summary>
        public enum EXRCompressionType
        {
            /// <summary>
            /// No compression.
            /// </summary>
            None,
            /// <summary>
            /// Run-length encoding compression.
            /// </summary>
            RLE,
            /// <summary>
            /// Zip compression (individual scanlines).
            /// </summary>
            Zip,
            /// <summary>
            /// Wavelet compression.
            /// </summary>
            PIZ,
            /// <summary>
            /// Zip compression (16 scanlines).
            /// </summary>
            Zips,
            /// <summary>
            /// Fixed compression size designed for realtime playback.
            /// </summary>
            B44,
            /// <summary>
            ///  Same as B44, but areas of flat color are further compressed.
            /// </summary>
            B44a,
            /// <summary>
            /// Dreamworks animation compression (32 scanlines).
            /// </summary>
            DWAA,
            /// <summary>
            /// Dreamworks animation compression (256 scanlines).
            /// </summary>
            DWAB,
        }

        /// <summary>
        /// Checks whether this package supports a compression type
        /// </summary>
        /// <param name="compressionType">EXRCompressionType to check for</param>
        /// <returns>True if the compression type is supported</returns>
        internal static bool SupportsCompressionLevel(EXRCompressionType compressionType)
        {
            switch (compressionType)
            {
                case EXRCompressionType.DWAA:
                case EXRCompressionType.DWAB:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// DWA Compression Type Info object
        /// </summary>
        internal static class DWACompressionTypeInfo
        {
            /// <summary>
            /// DWA Compression Type Minimum value
            /// </summary>
            public static readonly int k_MinValue = 1;
            /// <summary>
            /// DWA Compression Type Maximum value
            /// </summary>
            public static readonly int k_MaxValue = 100000;
            /// <summary>
            /// DWA Compression Type default value
            /// </summary>
            public static readonly int k_DefaultValue = 45;
        }

        internal static Texture2D.EXRFlags ToNativeType(EXRCompressionType type)
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
                case EXRCompressionType.PIZ:
                    nativeType = Texture2D.EXRFlags.CompressPIZ;
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
