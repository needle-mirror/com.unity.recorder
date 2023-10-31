#if HDRP_AVAILABLE
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition.Attributes;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Recorder.Input
{
    class AOVCameraAOVRequestAPIInput : CameraInput
    {
        private RTHandle[] m_RenderTextures;
        internal event Action waitForAsyncTasks;

        internal RTHandle[] AovTextures => m_RenderTextures;

        internal class AOVInfo
        {
            public AOVBuffers AOVBuffer;
            public AOVRequest AOVRequest;
            public int ChannelsCount = 3;
            public bool NeedAlpha;

            public GraphicsFormat WorkingTextureFormat
            {
                get
                {
                    switch (ChannelsCount)
                    {
                        case 1:
                            return GraphicsFormat.R16_SFloat;
                        case 2:
                            return GraphicsFormat.R16G16_SFloat;
                        default:
                            return GraphicsFormat.R16G16B16A16_SFloat;
                    }
                }
            }
        }

        // The dictionary of supported AOV types
        internal static readonly Dictionary<AOVType, AOVInfo> AOVInfoLookUp = new()
        {
            {
                AOVType.Beauty,
                new AOVInfo
                {
                    AOVRequest = new AOVRequest(AOVRequest.NewDefault()),
                    AOVBuffer = AOVBuffers.Output,
                    ChannelsCount = 4,
                    NeedAlpha  = true
                }
            },
            {
                AOVType.Albedo,
                new AOVInfo
                {
                    AOVRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(MaterialSharedProperty.Albedo),
                    AOVBuffer = AOVBuffers.Color
                }
            },
            {
                AOVType.Normal,
                new AOVInfo
                {
                    AOVRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(MaterialSharedProperty.Normal),
                    AOVBuffer = AOVBuffers.Color
                }
            },
            {
                AOVType.Smoothness,
                new AOVInfo
                {
                    AOVRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(MaterialSharedProperty.Smoothness),
                    AOVBuffer = AOVBuffers.Color,
                    ChannelsCount = 1
                }
            },
            {
                AOVType.AmbientOcclusion,
                new AOVInfo
                {
                    AOVRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(DebugFullScreen.ScreenSpaceAmbientOcclusion),
                    AOVBuffer = AOVBuffers.Output,
                    ChannelsCount = 1
                }
            },
            {
                AOVType.Metal,
                new AOVInfo
                {
                    AOVRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(MaterialSharedProperty.Metal),
                    AOVBuffer = AOVBuffers.Color,
                    ChannelsCount = 1
                }
            },
            {
                AOVType.Specular,
                new AOVInfo
                {
                    AOVRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(MaterialSharedProperty.Specular),
                    AOVBuffer = AOVBuffers.Color,
                }
            },
            {
                AOVType.Alpha,
                new AOVInfo
                {
                    AOVRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(MaterialSharedProperty.Alpha),
                    AOVBuffer = AOVBuffers.Color,
                    ChannelsCount = 1
                }
            },
            {
                AOVType.DirectDiffuse,
                new AOVInfo
                {
                    AOVRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(LightingProperty.DirectDiffuseOnly),
                    AOVBuffer = AOVBuffers.Color
                }
            },
            {
                AOVType.DirectSpecular,
                new AOVInfo
                {
                    AOVRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(LightingProperty.DirectSpecularOnly),
                    AOVBuffer = AOVBuffers.Color
                }
            },
            {
                AOVType.IndirectDiffuse,
                new AOVInfo
                {
                    AOVRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(LightingProperty.IndirectDiffuseOnly),
                    AOVBuffer = AOVBuffers.Color
                }
            },
            {
                AOVType.Reflection,
                new AOVInfo
                {
                    AOVRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(LightingProperty.ReflectionOnly),
                    AOVBuffer = AOVBuffers.Color
                }
            },
            {
                AOVType.Refraction,
                new AOVInfo
                {
                    AOVRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(LightingProperty.RefractionOnly),
                    AOVBuffer = AOVBuffers.Color
                }
            },
            {
                AOVType.Emissive,
                new AOVInfo
                {
                    AOVRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(LightingProperty.EmissiveOnly),
                    AOVBuffer = AOVBuffers.Color
                }
            },
            {
                AOVType.MotionVectors,
                new AOVInfo
                {
                    AOVRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(DebugFullScreen.MotionVectors),
                    AOVBuffer = AOVBuffers.MotionVectors,
                    ChannelsCount = 3
                }
            },
            {
                AOVType.Depth,
                new AOVInfo
                {
                    AOVRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(DebugFullScreen.Depth),
                    AOVBuffer = AOVBuffers.DepthStencil,
                    ChannelsCount = 1
                }
            }
        };


        void EnableAOVCapture(RecordingSession session, Camera cam)
        {
            var aovRecorderSettings = session.settings as AOVRecorderSettings;

            if (aovRecorderSettings != null)
            {
                var hdAdditionalCameraData = cam.GetComponent<HDAdditionalCameraData>();
                if (hdAdditionalCameraData != null)
                {
                    var aovRequestBuilder = new AOVRequestBuilder();
                    var aovTypesCount = aovRecorderSettings.GetAOVSelection().Length;

                    if (m_RenderTextures == null || m_RenderTextures.Length != aovTypesCount)
                    {
                        m_RenderTextures = new RTHandle[aovTypesCount];
                    }

                    var aovs = aovRecorderSettings.GetAOVSelection();
                    for (var i = 0; i < aovTypesCount; i++)
                    {
                        var aovRequest = new AOVRequest(AOVRequest.NewDefault());
                        var aovBuffer = AOVBuffers.Color;

                        if (AOVInfoLookUp.TryGetValue(aovs[i], out var aovInfo))
                        {
                            aovBuffer = aovInfo.AOVBuffer;
                            aovRequest = aovInfo.AOVRequest;
                        }
                        else
                        {
                            Debug.LogError($"Unrecognized AOV '{aovs[i]}'");
                        }

                        RTHandle currColorRT;

                        if (m_RenderTextures[i] == null)
                        {
                            currColorRT = RTHandles.Alloc(OutputWidth, OutputHeight,
                                colorFormat: aovInfo.WorkingTextureFormat, name: aovRecorderSettings.GetAOVSelection()[i].ToString());

                            m_RenderTextures[i] = currColorRT;
                        }
                        else
                        {
                            currColorRT = m_RenderTextures[i];
                        }
                        aovRequestBuilder.Add(aovRequest,
                            bufferId => currColorRT,
                            null,
                            new[] {aovBuffer},
                            (cmd, textures, properties) => {});
                    }


                    var aovRequestDataCollection = aovRequestBuilder.Build();
                    var previousRequests = hdAdditionalCameraData.aovRequests;
                    if (previousRequests != null && previousRequests.Any())
                    {
                        var listOfRequests = previousRequests.ToList();
                        foreach (var p in aovRequestDataCollection)
                        {
                            listOfRequests.Add(p);
                        }
                        var allRequests = new AOVRequestDataCollection(listOfRequests);
                        hdAdditionalCameraData.SetAOVRequests(allRequests);
                    }
                    else
                    {
                        hdAdditionalCameraData.SetAOVRequests(aovRequestDataCollection);
                    }
                }
                else
                {
                    Debug.LogError($"The '{cam.name}' AOV Recorder's camera is missing an HDAdditionalCameraData component");
                }
            }
        }

        void DisableAOVCapture(RecordingSession session)
        {
            var aovRecorderSettings = session.settings as AOVRecorderSettings;

            if (aovRecorderSettings != null)
            {
                var add = TargetCamera.GetComponent<HDAdditionalCameraData>();
                if (add != null)
                {
                    add.SetAOVRequests(null);
                }
            }
        }

        protected internal override void NewFrameStarting(RecordingSession session)
        {
            base.NewFrameStarting(session);
            EnableAOVCapture(session, TargetCamera);
        }

        protected internal override void FrameDone(RecordingSession session)
        {
            base.FrameDone(session);
            DisableAOVCapture(session);
        }

        protected internal override void EndRecording(RecordingSession session)
        {
            waitForAsyncTasks?.Invoke();
            base.EndRecording(session);

            if (m_RenderTextures != null)
            {
                foreach (var tuple in m_RenderTextures)
                {
                    if (tuple != null)
                    {
                        UnityHelpers.Destroy(tuple);
                    }
                }

                m_RenderTextures = null;
            }
        }
    }
}
#else // HDRP_AVAILABLE
namespace UnityEditor.Recorder.Input
{
    class AOVCameraDebugFrameworkInput : CameraInput
    {
        // nop No HDRP available
    }
}
#endif
