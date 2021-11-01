using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Recorder.Encoder
{
    /// <summary>
    /// The convention of the coordinate system for an encoder, to ensure that the images supplied to the encoder are flipped if needed.
    /// </summary>
    public enum EncoderCoordinateConvention
    {
        /// <summary>
        /// The origin is in the top left corner of each frame.
        /// </summary>
        OriginIsTopLeft,
        /// <summary>
        /// The origin is in the bottom left corner of each frame.
        /// </summary>
        OriginIsBottomLeft,
    }

    /// <summary>
    /// Interface to implement to create settings for an encoder.
    /// </summary>
    public interface IEncoderSettings
    {
        /// <summary>
        /// Returns the extension of the files to encode, in lowercase, without leading dot.
        /// </summary>
        string Extension { get; }

        /// <summary>
        /// Indicates where the first pixel of the image will be in the frame.
        /// </summary>
        EncoderCoordinateConvention CoordinateConvention => EncoderCoordinateConvention.OriginIsTopLeft;

        /// <summary>
        /// Indicates whether this encoder instance can support an alpha channel or not.
        /// </summary>
        bool CanCaptureAlpha { get; }

        /// <summary>
        /// Indicates whether this encoder instance can support an audio signal or not.
        /// </summary>
        bool CanCaptureAudio { get; }

        /// <summary>
        /// Gets the texture format this encoder requires from the Recorder.
        /// </summary>
        /// <param name="inputContainsAlpha">Whether the encoder's input contains an alpha channel or not.</param>
        /// <returns></returns>
        TextureFormat GetTextureFormat(bool inputContainsAlpha);

        /// <summary>
        /// Populates the lists of errors and warnings for a given encoder context.
        /// </summary>
        /// <param name="ctx">The context of the current recording.</param>
        /// <param name="errors">The list of errors to append to.</param>
        /// <param name="warnings">The list of warnings to append to.</param>
        void ValidateRecording(RecordingContext ctx, List<string> errors, List<string> warnings);

        /// <summary>
        /// Indicates whether this encoder requires the width and height of the image to both be even numbers or not.
        /// </summary>
        /// <returns>True if this encoder requires even dimensions, False otherwise.</returns>
        public bool RequiresEvenResolution()
        {
            return false;
        }

        /// <summary>
        /// Indicates whether the encoder is supported on the current operating system or not.
        /// </summary>
        /// <returns>True if the encoder works on this platform, False otherwise.</returns>
        bool SupportsCurrentPlatform();
    }
}
