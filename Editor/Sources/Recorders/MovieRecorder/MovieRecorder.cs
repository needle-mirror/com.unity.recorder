using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Recorder.Input;
using UnityEditor.Media;
using UnityEditor.Recorder.Encoder;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Recorder
{
    class MovieRecorder : BaseTextureRecorder<MovieRecorderSettings>
    {
        IEncoder m_Encoder;

        // The count of concurrent Movie Recorder instances. It is used to log a warning.
        static private int s_ConcurrentCount = 0;

        // Whether or not a warning was logged for concurrent movie recorders.
        static private bool s_WarnedUserOfConcurrentCount = false;

        // Whether or not recording was started properly.
        private bool m_RecordingStartedProperly = false;

        // Whether or not the recording has already been ended. To avoid messing with the count of concurrent recorders.
        private bool m_RecordingAlreadyEnded = false;

        private PooledBufferAsyncGPUReadback asyncReadback;
        protected override TextureFormat ReadbackTextureFormat => Settings.EncoderSettings.GetTextureFormat(Settings.CaptureAlpha && Settings.EncoderSettings.CanCaptureAlpha && Settings.ImageInputSettings.SupportsTransparent);

        protected internal override void SessionCreated(RecordingSession session)
        {
            base.SessionCreated(session);
            var audioInput = m_Inputs[1] as AudioInput;
            audioInput.NeedToCaptureAudio = () => Settings.EncoderSettings != null && Settings.EncoderSettings.CanCaptureAudio;
        }

        protected internal override bool BeginRecording(RecordingSession session)
        {
            m_RecordingStartedProperly = false;
            if (!base.BeginRecording(session))
                return false;

            try
            {
                Settings.fileNameGenerator.CreateDirectory(session);
            }
            catch (Exception)
            {
                ConsoleLogMessage($"Unable to create the output directory \"{Settings.fileNameGenerator.BuildAbsolutePath(session)}\".", LogType.Error);
                Recording = false;
                return false;
            }

            var input = m_Inputs[0] as BaseRenderTextureInput;
            if (input == null)
            {
                ConsoleLogMessage("Movie Recorder could not find its input.", LogType.Error);
                Recording = false;
                return false;
            }
            int width = input.OutputWidth;
            int height = input.OutputHeight;
            var audioInput = (AudioInput)m_Inputs[1];

            // Create the encoder
            m_Encoder = EncoderTypeUtilities.CreateEncoderInstance(Settings.EncoderSettings.GetType());

            var frameRate = session.settings.FrameRatePlayback == FrameRatePlayback.Constant
                ? RationalFromDouble(session.settings.FrameRate)
                : new MediaRational { numerator = 0, denominator = 0 };
            var lsErrors = new List<string>();
            var lsWarnings = new List<string>();

            // Get a recording context
            var recordingContext = Settings.GetRecordingContext();
            // Set locally determined fields
            recordingContext.path = Settings.fileNameGenerator.BuildAbsolutePath(session);
            recordingContext.fps = frameRate;

            // Update the context and detect errors
            Settings.EncoderSettings.ValidateRecording(recordingContext, lsErrors, lsWarnings);
            if (lsErrors.Count > 0)
            {
                foreach (var e in lsErrors)
                    ConsoleLogMessage(e, LogType.Error);
                Recording = false;
                return false;
            }

            asyncReadback = new PooledBufferAsyncGPUReadback();
            // Show warnings
            foreach (var w in lsWarnings)
            {
                ConsoleLogMessage(w, LogType.Warning);
            }

            try
            {
                m_Encoder.OpenStream(Settings.EncoderSettings, recordingContext);
            }
            catch (Exception ex)
            {
                ConsoleLogMessage($"Unable to create encoder: '{ex.Message}'", LogType.Error);
                Recording = false;
                return false;
            }

            if (RecorderOptions.VerboseMode)
                ConsoleLogMessage(
                    $"MovieRecorder starting to write video {width}x{height}@[{recordingContext.fps.numerator}/{recordingContext.fps.denominator}] fps into {Settings.fileNameGenerator.BuildAbsolutePath(session)}",
                    LogType.Log);

            if (audioInput.AudioSettings.PreserveAudio && !UnityHelpers.CaptureAccumulation(settings))
            {
                if (RecorderOptions.VerboseMode)
                    ConsoleLogMessage($"Starting to write audio {audioInput.ChannelCount}ch @ {audioInput.SampleRate}Hz", LogType.Log);
            }
            else
            {
                if (RecorderOptions.VerboseMode)
                    ConsoleLogMessage("Starting with no audio.", LogType.Log);
            }

            s_ConcurrentCount++;
            m_RecordingStartedProperly = true;
            m_RecordingAlreadyEnded = false;
            return true;
        }

        protected internal override void RecordFrame(RecordingSession session)
        {
            if (m_Inputs.Count != 2)
                throw new Exception("Unsupported number of sources");

            if (!m_RecordingStartedProperly)
                return; // error will have been triggered in BeginRecording()

            base.RecordFrame(session);
        }

        protected internal override void EndRecording(RecordingSession session)
        {
            if (asyncReadback != null)
            {
                asyncReadback.Dispose();
                asyncReadback = null;
            }

            if (m_Encoder != null)
            {
                m_Encoder.CloseStream();
                m_Encoder = null;
            }

            base.EndRecording(session);

            if (m_RecordingStartedProperly && !m_RecordingAlreadyEnded)
            {
                s_ConcurrentCount--;
                if (s_ConcurrentCount < 0)
                    ConsoleLogMessage($"Recording ended with no matching beginning recording.", LogType.Error);
                if (s_ConcurrentCount <= 1 && s_WarnedUserOfConcurrentCount)
                    s_WarnedUserOfConcurrentCount = false; // reset so that we can warn at the next occurence
                m_RecordingAlreadyEnded = true;
            }
        }

        internal override bool WriteGPUTextureFrame(RenderTexture tex)
        {
            if (m_Encoder.GetVideoInputPath == IEncoder.VideoInputPath.GPUBuffer)
            {
                var recorderTime = DequeueTimeStamp();
                m_Encoder.AddVideoFrame(tex, ComputeMediaTime(recorderTime));
            }
            else
            {
                asyncReadback.RequestGPUReadBack(tex, GraphicsFormatUtility.GetGraphicsFormat(ReadbackTextureFormat, false), WriteCPUFrame);
            }

            WarnOfConcurrentRecorders();
            return true;
        }

        void WriteCPUFrame(AsyncGPUReadbackRequest r)
        {
            if (r.hasError)
            {
                ConsoleLogMessage("The rendered image has errors. Skipping this frame.", LogType.Error);
                return;
            }
            var recorderTime = DequeueTimeStamp();
            m_Encoder.AddVideoFrame(r.GetData<byte>(), ComputeMediaTime(recorderTime));
        }

        internal override void RecordSubFrame(RecordingSession ctx)
        {
            base.RecordSubFrame(ctx);
            var audioInput = (AudioInput)m_Inputs[1];
            var okCaptureAccum = Settings.AccumulationSettings.CaptureAccumulation && accumulationInitialized;
            if (Settings.CaptureAudio && Settings.EncoderSettings.CanCaptureAudio &&
                audioInput.AudioSettings.PreserveAudio &&
                (okCaptureAccum || !Settings.AccumulationSettings.CaptureAccumulation))
            {
                m_Encoder.AddAudioFrame(audioInput.MainBuffer);
            }
        }

        private void WarnOfConcurrentRecorders()
        {
            if (s_ConcurrentCount > 1 && !s_WarnedUserOfConcurrentCount)
            {
                ConsoleLogMessage($"There are two or more concurrent Movie Recorders in your project. You should keep only one of them active per recording to avoid experiencing slowdowns or other issues.", LogType.Warning);
                s_WarnedUserOfConcurrentCount = true;
            }
        }

        private MediaTime ComputeMediaTime(float recorderTime)
        {
            if (Settings.FrameRatePlayback == FrameRatePlayback.Constant)
                return new Media.MediaTime { count = 0, rate = Media.MediaRational.Invalid };

            const uint kUSPerSecond = 10000000;
            long count = (long)((double)recorderTime * (double)kUSPerSecond);
            return new Media.MediaTime(count, kUSPerSecond);
        }

        // https://stackoverflow.com/questions/26643695/converting-decimal-to-fraction-c
        static long GreatestCommonDivisor(long a, long b)
        {
            if (a == 0)
                return b;

            if (b == 0)
                return a;

            return (a < b) ? GreatestCommonDivisor(a, b % a) : GreatestCommonDivisor(b, a % b);
        }

        internal static MediaRational RationalFromDouble(double value)
        {
            if (double.IsNaN(value))
                return new MediaRational { numerator = 0, denominator = 0 };
            if (double.IsPositiveInfinity(value))
                return new MediaRational { numerator = Int32.MaxValue, denominator = 1 };
            if (double.IsNegativeInfinity(value))
                return new MediaRational { numerator = Int32.MinValue, denominator = 1 };

            var integral = Math.Floor(value);
            var frac = value - integral;

            const long precision = 10000000;

            var gcd = GreatestCommonDivisor((long)Math.Round(frac * precision), precision);
            var denom = precision / gcd;

            return new MediaRational
            {
                numerator = (int)((long)integral * denom + ((long)Math.Round(frac * precision)) / gcd),
                denominator = (int)denom
            };
        }

        internal static double DoubleFromRational(MediaRational rational)
        {
            if (rational.denominator == 0)
                return 0;
            else
                return (float)rational.numerator / (float)rational.denominator;
        }
    }
}
