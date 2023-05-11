using System;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
#if HDRP_AVAILABLE
using UnityEngine.Rendering.HighDefinition;
#endif
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Recorder
{
    /// <summary>
    /// Abstract base class for all Recorders that output images.
    /// </summary>
    /// <typeparam name="T">The class implementing the Recorder Settings.</typeparam>
    public abstract class BaseTextureRecorder<T> : GenericRecorder<T> where T : RecorderSettings
    {
        /// <summary>
        /// Whether or not to use asynchronous GPU commands in order to get the texture for the recorder.
        /// </summary>
        protected bool UseAsyncGPUReadback;

        /// <summary>
        /// Whether or not accumulation is requested and has been enabled.
        /// </summary>
        internal bool accumulationInitialized;

        private PooledBufferAsyncGPUReadback asyncReadback;
#if HDRP_AVAILABLE
        bool m_AccumulationIsActive;
        int m_SubFrameIndex;
        int m_NumSubFrames;
        Vector2[] m_JitterOffsets;
        Vector2 m_CurrentJitterOffset;

        struct SavedCameraProperties
        {
            public bool usePhysicalProperties;
        }

        readonly Dictionary<Camera, SavedCameraProperties> m_NonJitteredProjections = new Dictionary<Camera, SavedCameraProperties>();
#if HDRP_14_0_2_AVAILABLE
        // Wraps the callback overriding the spotlight view matrix computation.
        // We need to maintain a reference to the light data while evaluating the view,
        // which is not anticipated by the API (callback signature).
        class CustomViewCallbackWrapper : IDisposable
        {
            Matrix4x4 m_ViewRotationMatrix = Matrix4x4.identity;
            HDAdditionalLightData m_AdditionalLightData;

            public Matrix4x4 ViewRotationMatrix
            {
                set => m_ViewRotationMatrix = value;
            }

            public CustomViewCallbackWrapper(HDAdditionalLightData additionalLightData)
            {
                m_AdditionalLightData = additionalLightData;
                m_AdditionalLightData.CustomViewCallbackEvent += GetViewMatrix;
            }

            public void Dispose()
            {
                // In case the component was destroyed during recording. Unexpected but possible.
                if (m_AdditionalLightData != null)
                {
                    m_AdditionalLightData.CustomViewCallbackEvent -= GetViewMatrix;
                    m_AdditionalLightData = null;
                }
            }

            Matrix4x4 GetViewMatrix(Matrix4x4 localToWorldMatrix)
            {
                var invView = localToWorldMatrix * m_ViewRotationMatrix;
                var view = invView.inverse;

                // Note that camera space matches OpenGL convention: camera's forward is the negative Z axis.
                // This is different from Unity's convention, where forward is the positive Z axis.
                view.m20 *= -1f;
                view.m21 *= -1f;
                view.m22 *= -1f;
                view.m23 *= -1f;

                return view;
            }
        }

        readonly List<CustomViewCallbackWrapper> m_SpotLightViewCallbacks = new List<CustomViewCallbackWrapper>();
#endif
#endif
        Texture2D m_ReadbackTexture;
        readonly Queue<float> m_AsyncReadbackTimeStamps = new Queue<float>();


        internal void EnqueueTimeStamp(float time)
        {
            m_AsyncReadbackTimeStamps.Enqueue(time);
        }

        internal float DequeueTimeStamp()
        {
            if (m_AsyncReadbackTimeStamps.Count == 0)
            {
                throw new Exception("Timestamp queue is empty");
            }

            return m_AsyncReadbackTimeStamps.Dequeue();
        }

        /// <summary>
        /// Stores the format of the texture used for the readback.
        /// </summary>
        protected abstract TextureFormat ReadbackTextureFormat { get; }

        /// <inheritdoc/>
        protected internal override bool BeginRecording(RecordingSession session)
        {
            if (!base.BeginRecording(session))
                return false;
            UseAsyncGPUReadback = SystemInfo.supportsAsyncGPUReadback;
            m_AsyncReadbackTimeStamps.Clear();
            asyncReadback = new PooledBufferAsyncGPUReadback();
            return true;
        }

        void SetupAccumulation()
        {
#if HDRP_AVAILABLE
            if (!accumulationInitialized && RenderPipelineManager.currentPipeline is HDRenderPipeline hdRenderPipeline)
            {
                if (settings.IsAccumulationSupported() && settings is IAccumulation accumulation)
                {
                    AccumulationSettings aSettings = accumulation.GetAccumulationSettings();

                    // If Samples = 1, we need no accumulation, nor should we modify the projection.
                    m_AccumulationIsActive =
                        aSettings != null && aSettings.CaptureAccumulation && aSettings.Samples > 1;

                    if (m_AccumulationIsActive)
                    {
                        m_NumSubFrames = aSettings.Samples;
                        m_SubFrameIndex = -1;
                        m_CurrentJitterOffset = Vector2.zero;

                        // Cache jitter offsets if needed
                        // Note that with a pseudo random sequence we don't need to regenerate if m_NumSubFrames shrinks.
                        // These offsets are used both for subpixel AA and shadowmap AA.
                        if (m_JitterOffsets == null || m_JitterOffsets.Length < m_NumSubFrames)
                        {
                            m_JitterOffsets = new Vector2[m_NumSubFrames];
                            HammersleySequence.GetPoints(m_JitterOffsets);

                            // [0, 1] to [-0.5, 0.5] range.
                            for (var i = 0; i != m_JitterOffsets.Length; ++i)
                            {
                                m_JitterOffsets[i] -= Vector2.one * 0.5f;
                            }
                        }

#if HDRP_14_0_2_AVAILABLE
                        // Shadowmap rotation
                        foreach (var lightData in FindObjectsByType<HDAdditionalLightData>(FindObjectsSortMode.None))
                        {
                            // Shadowmap rotation only supports cone shaped spot-light at the moment.
                            var light = lightData.GetComponent<Light>();
                            if (light.type == LightType.Spot)
                            {
                                m_SpotLightViewCallbacks.Add(new CustomViewCallbackWrapper(lightData));
                            }
                        }
#endif

                        if (aSettings.UseSubPixelJitter)
                        {
                            RenderPipelineManager.beginContextRendering += AssignJitteredMatrices;
                            RenderPipelineManager.endContextRendering += RestoreNonJitteredMatrices;
                        }

                        if (aSettings.ShutterType == AccumulationSettings.ShutterProfileType.Range)
                        {
                            hdRenderPipeline.BeginRecording(
                                aSettings.Samples,
                                aSettings.ShutterInterval,
                                aSettings.ShutterFullyOpen,
                                aSettings.ShutterBeginsClosing
                            );
                        }
                        else
                        {
                            hdRenderPipeline.BeginRecording(
                                aSettings.Samples,
                                aSettings.ShutterInterval,
                                aSettings.ShutterProfileCurve
                            );
                        }
                        accumulationInitialized = true;
                    }
                }
            }
#endif
        }

        /// <inheritdoc/>
        protected internal override void RecordFrame(RecordingSession session)
        {
            EnqueueTimeStamp(session.recorderTime);

            var input = (BaseRenderTextureInput)m_Inputs[0];

            if (input.ReadbackTexture != null)
            {
                WriteFrame(input.ReadbackTexture);
                return;
            }

            var renderTexture = input.OutputRenderTexture;

            if (renderTexture == null)
            {
                Debug.LogWarning($"Ignoring the current frame because the source has been disposed");
                return;
            }

            if (UseAsyncGPUReadback)
            {
                if (WriteGPUTextureFrame(renderTexture)) // Recorder might want ot
                {
                    return;
                }

                asyncReadback.RequestGPUReadBack(renderTexture, GraphicsFormatUtility.GetGraphicsFormat(ReadbackTextureFormat, false), ReadbackDone);
                return;
            }

            var width = renderTexture.width;
            var height = renderTexture.height;

            if (m_ReadbackTexture == null)
                m_ReadbackTexture = CreateReadbackTexture(width, height);

            var backupActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            m_ReadbackTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            m_ReadbackTexture.Apply();
            RenderTexture.active = backupActive;
            WriteFrame(m_ReadbackTexture);
        }

        internal virtual bool WriteGPUTextureFrame(RenderTexture tex)
        {
            return false;
        }

        void ReadbackDone(AsyncGPUReadbackRequest r)
        {
            Profiler.BeginSample("BaseTextureRecorder.ReadbackDone");
            WriteFrame(r);
            Profiler.EndSample();
        }

        // <summary>
        // Prepares a frame before recording it. Callback is invoked for every frame during the recording session, before RecordFrame.
        // </summary>
        // <param name="ctx">The current recording session.</param>
        protected internal override void PrepareNewFrame(RecordingSession ctx)
        {
            base.PrepareNewFrame(ctx);
#if HDRP_AVAILABLE
            SetupAccumulation();
            if (m_AccumulationIsActive && RenderPipelineManager.currentPipeline is HDRenderPipeline hdRenderPipeline)
            {
                m_SubFrameIndex = ++m_SubFrameIndex % m_NumSubFrames;

                // Note that we use the same pseudo random sequence than we use for subpixel AA.
                m_CurrentJitterOffset = m_JitterOffsets[m_SubFrameIndex];
#if HDRP_14_0_2_AVAILABLE
                // No need to shift the range since we work with angles.
                var angle = m_CurrentJitterOffset.x * 360f;
                var spotLightViewRotationMatrix = Matrix4x4.Rotate(Quaternion.AngleAxis(angle, Vector3.forward));
                foreach (var callback in m_SpotLightViewCallbacks)
                {
                    callback.ViewRotationMatrix = spotLightViewRotationMatrix;
                }
#endif
                hdRenderPipeline.PrepareNewSubFrame();
            }
#endif
        }

        /// <inheritdoc/>
        protected internal override void EndRecording(RecordingSession session)
        {
#if HDRP_AVAILABLE
#if HDRP_14_0_2_AVAILABLE
            // Remove light data callbacks.
            foreach (var callback in m_SpotLightViewCallbacks)
            {
                callback.Dispose();
            }
            m_SpotLightViewCallbacks.Clear();
#endif
            // Remove render-pipeline callbacks regardless of whether they were added, no error will be thrown.
            RenderPipelineManager.beginContextRendering -= AssignJitteredMatrices;
            RenderPipelineManager.endContextRendering -= RestoreNonJitteredMatrices;

            // In the unlikely event EndRecording is invoked in the middle of rendering.
            RestoreNonJitteredMatrices();

            if (m_AccumulationIsActive && RenderPipelineManager.currentPipeline is HDRenderPipeline hdRenderPipeline)
            {
                // hdPipeline.EndRecording needs to be called before base.EndRecording because
                // it will restore the Time.captureFrameRate
                // this would otherwise override what needs to be done by base.EndRecording
                hdRenderPipeline.EndRecording();
            }

            m_AccumulationIsActive = false;
#endif
            if (asyncReadback != null)
            {
                asyncReadback.Dispose();
                asyncReadback = null;
            }

            base.EndRecording(session);


            DisposeEncoder();
        }

        private Texture2D CreateReadbackTexture(int width, int height)
        {
            return new Texture2D(width, height, ReadbackTextureFormat, false);
        }

        /// <summary>
        /// Writes the frame from an asynchronous GPU read request.
        /// </summary>
        /// <param name="r">The asynchronous readback target.</param>
        protected virtual void WriteFrame(AsyncGPUReadbackRequest r)
        {
            if (r.hasError)
            {
                ConsoleLogMessage("The rendered image has errors. Skipping this frame.", LogType.Error);
                return;
            }

            if (m_ReadbackTexture == null)
                m_ReadbackTexture = CreateReadbackTexture(r.width, r.height);
            Profiler.BeginSample("BaseTextureRecorder.LoadRawTextureData");
            m_ReadbackTexture.LoadRawTextureData(r.GetData<byte>());
            Profiler.EndSample();
            WriteFrame(m_ReadbackTexture);
        }

        /// <summary>
        /// Writes the frame from a Texture2D.
        /// </summary>
        /// <param name="t">The readback target.</param>
        protected virtual void WriteFrame(Texture2D t)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Releases the encoder resources.
        /// </summary>
        protected virtual void DisposeEncoder()
        {
            UnityHelpers.Destroy(m_ReadbackTexture);
            Recording = false;
        }

#if HDRP_AVAILABLE
        void AssignJitteredMatrices(ScriptableRenderContext _, List<Camera> cameras)
        {
            // Save original projection matrices and assigned the jittered ones.
            foreach (var camera in cameras)
            {
                // We only jitter the projection of game cameras, previews, scene view, etc... are not affected.
                if (camera.cameraType == CameraType.Game)
                {
                    var originalProjection = camera.projectionMatrix;
                    m_NonJitteredProjections.Add(camera, new SavedCameraProperties
                    {
                        usePhysicalProperties = camera.usePhysicalProperties
                    });
                    camera.projectionMatrix = GetJitteredProjectionMatrix(camera, originalProjection, m_CurrentJitterOffset);
                }
            }
        }

        void RestoreNonJitteredMatrices(ScriptableRenderContext context, List<Camera> cameras)
        {
            RestoreNonJitteredMatrices();
        }

        void RestoreNonJitteredMatrices()
        {
            foreach (var(camera, camProperties) in m_NonJitteredProjections)
            {
                camera.ResetProjectionMatrix();
                camera.usePhysicalProperties = camProperties.usePhysicalProperties;
            }

            m_NonJitteredProjections.Clear();
        }

        // Similar to HDRP TAA implementation.
        static Matrix4x4 GetJitteredProjectionMatrix(Camera camera, Matrix4x4 originalProjection, Vector2 jitter)
        {
            var actualWidth = camera.pixelWidth;
            var actualHeight = camera.pixelHeight;

            if (camera.orthographic)
            {
                var vertical = camera.orthographicSize;
                var horizontal = vertical * camera.aspect;

                jitter.x *= horizontal / (0.5f * actualWidth);
                jitter.y *= vertical / (0.5f * actualHeight);

                var left = jitter.x - horizontal;
                var right = jitter.x + horizontal;
                var top = jitter.y + vertical;
                var bottom = jitter.y - vertical;

                return Matrix4x4.Ortho(left, right, bottom, top, camera.nearClipPlane, camera.farClipPlane);
            }

            var planes = originalProjection.decomposeProjection;

            var verticalFov = Math.Abs(planes.top) + Math.Abs(planes.bottom);
            var horizontalFov = Math.Abs(planes.left) + Math.Abs(planes.right);

            var planeJitter = new Vector2(jitter.x * horizontalFov / actualWidth, jitter.y * verticalFov / actualHeight);

            planes.left += planeJitter.x;
            planes.right += planeJitter.x;
            planes.top += planeJitter.y;
            planes.bottom += planeJitter.y;

            // Reconstruct the far plane for the jittered matrix.
            // For extremely high far clip planes, the decomposed projection zFar evaluates to infinity.
            if (float.IsInfinity(planes.zFar))
            {
                planes.zFar = camera.farClipPlane;
            }

            return Matrix4x4.Frustum(planes);
        }

#endif
    }
}
