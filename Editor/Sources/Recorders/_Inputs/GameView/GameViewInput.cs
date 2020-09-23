using System;
using UnityEditor.Recorder;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditor.Recorder.Input
{
    class GameViewInput : BaseRenderTextureInput
    {
        bool m_ModifiedResolution;
        TextureFlipper m_VFlipper;
        RenderTexture m_CaptureTexture;

        GameViewInputSettings scSettings
        {
            get { return (GameViewInputSettings)settings; }
        }

        protected internal override void NewFrameReady(RecordingSession session)
        {
            Profiler.BeginSample("GameViewInput.NewFrameReady");
#if UNITY_2019_1_OR_NEWER
            ScreenCapture.CaptureScreenshotIntoRenderTexture(m_CaptureTexture);
            var movieRecorderSettings = session.settings as MovieRecorderSettings;
            bool needToFlip = scSettings.FlipFinalOutput;
            if (movieRecorderSettings != null)
            {
                bool encoderAlreadyFlips = movieRecorderSettings.encodersRegistered[movieRecorderSettings.encoderSelected].PerformsVerticalFlip;
                needToFlip &= encoderAlreadyFlips;
            }
            if (needToFlip)
                m_VFlipper?.Flip(m_CaptureTexture);
#else
            ReadbackTexture = ScreenCapture.CaptureScreenshotAsTexture();
            var movieRecorderSettings = session.settings as MovieRecorderSettings;
            if (movieRecorderSettings != null)
            {
                var currEncoder = movieRecorderSettings.encodersRegistered[movieRecorderSettings.encoderSelected];
                var requiredFormat = currEncoder.GetTextureFormat(movieRecorderSettings);
                var isGameView = movieRecorderSettings.ImageInputSettings is GameViewInputSettings;
                if (!currEncoder.PerformsVerticalFlip)
                {
                    ReadbackTexture = UnityHelpers.FlipTextureVertically(ReadbackTexture, movieRecorderSettings.CaptureAlpha);
                }
                if (requiredFormat != ReadbackTexture.format)
                {
                    if (requiredFormat == TextureFormat.RGB24 && ReadbackTexture.format == TextureFormat.RGBA32)
                        ReadbackTexture = UnityHelpers.RGBA32_to_RGB24(ReadbackTexture);
                    else
                        throw new Exception($"Unexpected conversion requested: from {ReadbackTexture.format} to {requiredFormat}.");
                }
            }
#endif
            Profiler.EndSample();
        }

        protected internal override void BeginRecording(RecordingSession session)
        {
            OutputWidth = scSettings.OutputWidth;
            OutputHeight = scSettings.OutputHeight;

            int w, h;
            GameViewSize.GetGameRenderSize(out w, out h);
            if (w != OutputWidth || h != OutputHeight)
            {
                var size = GameViewSize.SetCustomSize(OutputWidth, OutputHeight) ?? GameViewSize.AddSize(OutputWidth, OutputHeight);
                if (GameViewSize.modifiedResolutionCount == 0)
                    GameViewSize.BackupCurrentSize();
                else
                {
                    if (size != GameViewSize.currentSize)
                    {
                        Debug.LogError("Requesting a resolution change while a recorder's input has already requested one! Undefined behaviour.");
                    }
                }
                GameViewSize.modifiedResolutionCount++;
                m_ModifiedResolution = true;
                GameViewSize.SelectSize(size);
            }

#if !UNITY_2019_1_OR_NEWER
            // Before 2019.1, we capture synchronously into a Texture2D, so we don't need to create
            // a RenderTexture that is used for reading asynchronously.
            return;
#else
            m_CaptureTexture = new RenderTexture(OutputWidth, OutputHeight, 0, RenderTextureFormat.ARGB32)
            {
                wrapMode = TextureWrapMode.Repeat
            };
            m_CaptureTexture.Create();

            var movieRecorderSettings = session.settings as MovieRecorderSettings;
            bool needToFlip = scSettings.FlipFinalOutput;
            if (movieRecorderSettings != null)
            {
                bool encoderAlreadyFlips = movieRecorderSettings.encodersRegistered[movieRecorderSettings.encoderSelected].PerformsVerticalFlip;
                needToFlip &= encoderAlreadyFlips;
            }

            if (needToFlip)
            {
                m_VFlipper = new TextureFlipper(false);
                m_VFlipper.Init(m_CaptureTexture);
                OutputRenderTexture = m_VFlipper.workTexture;
            }
            else
                OutputRenderTexture = m_CaptureTexture;
#endif
        }

        protected internal override void FrameDone(RecordingSession session)
        {
            UnityHelpers.Destroy(ReadbackTexture);
            ReadbackTexture = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_ModifiedResolution)
                {
                    if (GameViewSize.modifiedResolutionCount > 0)
                        GameViewSize.modifiedResolutionCount--; // don't allow negative if called twice
                    if (GameViewSize.modifiedResolutionCount == 0)
                        GameViewSize.RestoreSize();
                }
            }

            m_VFlipper?.Dispose();
            m_VFlipper = null;

            base.Dispose(disposing);
        }
    }
}
