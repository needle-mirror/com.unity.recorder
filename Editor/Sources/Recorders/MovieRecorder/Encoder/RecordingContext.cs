using UnityEditor.Media;

namespace UnityEditor.Recorder.Encoder
{
    /// <summary>
    /// A structure that defines information that is passed to the encoder about the recording's inputs and requirements.
    /// </summary>
    public struct RecordingContext
    {
        /// <summary>
        /// The width of the recorded image.
        /// </summary>
        public int width;

        /// <summary>
        /// The height of the recorder image.
        /// </summary>
        public int height;

        /// <summary>
        /// The frame rate of the recording.
        /// </summary>
        public MediaRational fps;

        /// <summary>
        /// The type of frame rate configuration of the recording.
        /// </summary>
        public FrameRatePlayback frameRateMode;

        /// <summary>
        /// Whether or not the encoder should capture audio.
        /// </summary>
        public bool doCaptureAudio;

        /// <summary>
        /// Whether or not the encoder should capture the alpha channel.
        /// </summary>
        public bool doCaptureAlpha;

        /// <summary>
        /// The path of the output file.
        /// </summary>
        public string path;
    }
}
