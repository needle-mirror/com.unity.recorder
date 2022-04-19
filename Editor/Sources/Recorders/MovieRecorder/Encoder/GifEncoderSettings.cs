using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace UnityEditor.Recorder.Encoder
{
    /// <summary>
    /// The settings of the GIF Encoder.
    /// </summary>
    /// <remarks>
    /// This class is sealed because users shouldn't inherit from it. Instead, create a new encoder along with its settings class.
    /// </remarks>
    [DisplayName("GIF Encoder")]
    [Serializable]
    [EncoderSettings(typeof(GifEncoder))]
    public sealed class GifEncoderSettings : IEncoderSettings, IEquatable<GifEncoderSettings>
    {
        /// <inheritdoc/>
        string IEncoderSettings.Extension => "gif";

        /// <inheritdoc/>
        bool IEncoderSettings.CanCaptureAlpha => true;

        /// <inheritdoc/>
        bool IEncoderSettings.CanCaptureAudio => false;

        /// <summary>
        /// Indicates whether the generated file should loop the frame sequence indefinitely or not.
        /// </summary>
        public bool Loop
        {
            get => loop;
            set => loop = value;
        }
        [SerializeField] bool loop = true;

        /// <summary>
        /// The quality of the GIF.
        /// </summary>
        public uint Quality
        {
            get => quality;
            set
            {
                if (value < 1 || value > 100)
                    throw new ArgumentOutOfRangeException($"The quality attribute of the GIF encoder must have a value between 1 and 100.");
                quality = value;
            }
        }
        [SerializeField] uint quality = 90;

        /// <inheritdoc/>
        TextureFormat IEncoderSettings.GetTextureFormat(bool inputContainsAlpha)
        {
            return TextureFormat.RGBA32;
        }

        /// <inheritdoc/>
        void IEncoderSettings.ValidateRecording(RecordingContext ctx, List<string> errors, List<string> warnings)
        {
            if (ctx.doCaptureAudio)
                errors.Add($"The GIF encoder does not support audio tracks.");
        }

        /// <inheritdoc/>
        bool IEncoderSettings.SupportsCurrentPlatform()
        {
            return true;
        }

        /// <inheritdoc/>
        bool IEquatable<GifEncoderSettings>.Equals(GifEncoderSettings other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return loop == other.loop && quality == other.quality;
        }

        /// <summary>
        /// Compares the current object with another one.
        /// </summary>
        /// <param name="obj">The object to compare with the current one.</param>
        /// <returns>True if the two objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is GifEncoderSettings other && ((IEquatable<GifEncoderSettings>) this).Equals(other);
        }

        /// <summary>
        /// Returns a hash code of all serialized fields.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(loop, quality);
        }
    }
}
