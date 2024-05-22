using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if HDRP_AVAILABLE
using UnityEngine.Rendering.HighDefinition;
#endif
#if URP_AVAILABLE
using UnityEngine.Rendering.Universal;
#endif
using UnityObject = UnityEngine.Object;

namespace UnityEditor.Recorder
{
    /// <summary>
    /// An ad-hoc collection of helpers for the Recorders.
    /// </summary>
    public static class UnityHelpers
    {
        /// <summary>
        /// Allows destroying Unity.Objects.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="allowDestroyingAssets"></param>
        public static void Destroy(UnityObject obj, bool allowDestroyingAssets = false)
        {
            if (obj == null)
                return;

            if (EditorApplication.isPlaying)
                UnityObject.Destroy(obj);
            else
                UnityObject.DestroyImmediate(obj, allowDestroyingAssets);
        }

        internal static bool IsPlaying()
        {
            return EditorApplication.isPlaying;
        }

        internal static GameObject CreateRecorderGameObject(string name)
        {
            var gameObject = new GameObject(name) { tag = "EditorOnly" };
            SetGameObjectVisibility(gameObject, RecorderOptions.ShowRecorderGameObject);
            return gameObject;
        }

        internal static void SetGameObjectsVisibility(bool value)
        {
            var rcb = BindingManager.FindRecorderBindings();
            foreach (var rc in rcb)
            {
                SetGameObjectVisibility(rc.gameObject, value);
            }

            var rcs = FindObjectsHelper.FindObjectsByTypeWrapper<RecorderComponent>();
            foreach (var rc in rcs)
            {
                SetGameObjectVisibility(rc.gameObject, value);
            }
        }

        static void SetGameObjectVisibility(GameObject obj, bool visible)
        {
            if (obj != null)
            {
                obj.hideFlags = visible ? HideFlags.None : HideFlags.HideInHierarchy;

                if (!Application.isPlaying)
                {
                    try
                    {
                        EditorSceneManager.MarkSceneDirty(obj.scene);
                        EditorApplication.RepaintHierarchyWindow();
                        EditorApplication.DirtyHierarchyWindowSorting();
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        internal static bool AreAllSceneDataLoaded()
        {
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.isLoaded == false)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// A label including the version of the package, for use in encoder metadata tags for instance.
        /// </summary>
        internal static string PackageDescription
        {
            get
            {
                return "Recorder " + PackageVersion;
            }
        }

        private static ListRequest LsPackages = Client.List();
        private static string PackageVersion
        {
            get
            {
                if (m_PackageVersion.Length == 0)
                {
                    // Read the package version
                    var packageInfo = PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.recorder");
                    m_PackageVersion = packageInfo.version;
                }
                return m_PackageVersion;
            }
        }
        private static string m_PackageVersion = "";

        /// <summary>
        /// Convert an RGBA32 texture to an RGB24 one.
        /// </summary>
        /// <param name="tex"></param>
        /// <returns></returns>
        internal static Texture2D RGBA32_to_RGB24(Texture2D tex)
        {
            if (tex.format != TextureFormat.RGBA32)
                throw new System.Exception($"Expecting RGBA32 format, received {tex.format}");

            Texture2D newTex = new Texture2D(tex.width, tex.height, TextureFormat.RGB24, false);
            newTex.SetPixels(tex.GetPixels());
            newTex.Apply();

            return newTex;
        }

        /// <summary>
        /// Load an asset from the current package's Editor/Assets folder.
        /// </summary>
        /// <param name="relativeFilePathWithExtension">The relative filename inside the Editor/Assets folder, without
        /// leading slash.</param>
        /// <param name="logError">Set this flag to true if you need to log errors when the Recorder cannot find the asset.</param>
        /// <typeparam name="T">The type of asset to load</typeparam>
        /// <returns></returns>
        internal static T LoadLocalPackageAsset<T>(string relativeFilePathWithExtension, bool logError) where T : Object
        {
            T result = default(T);
            var fullPathInProject = $"Packages/com.unity.recorder/Editor/Assets/{relativeFilePathWithExtension}";

            if (File.Exists(fullPathInProject))
                result = AssetDatabase.LoadAssetAtPath(fullPathInProject, typeof(T)) as T;
            else if (logError)
                Debug.LogError($"Local asset file {fullPathInProject} not found.");
            return result;
        }

        /// <summary>
        /// Are we currently using the High Definition Render Pipeline.
        /// </summary>
        /// <returns>bool</returns>
        internal static bool UsingHDRP()
        {
            var pipelineAsset = GraphicsSettings.currentRenderPipeline;
            var usingHDRP = pipelineAsset != null && pipelineAsset.GetType().FullName.Contains("High");
            return usingHDRP;
        }

        /// <summary>
        /// Are we currently using the Universal Render Pipeline.
        /// </summary>
        /// <returns>bool</returns>
        internal static bool UsingURP()
        {
            var pipelineAsset = GraphicsSettings.currentRenderPipeline;
            var usingURP = pipelineAsset != null &&
                (pipelineAsset.GetType().FullName.Contains("Universal") ||
                    pipelineAsset.GetType().FullName.Contains("Lightweight"));
            return usingURP;
        }

        internal static bool UsingURP2DRenderer()
        {
#if URP_AVAILABLE && UNITY_2023_2_OR_NEWER
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

            if (urp == null)
                return false;

            foreach (var renderer in urp.renderers)
            {
                if (renderer == null)
                    continue;

                if (renderer.GetType().FullName.Contains("Renderer2D"))
                    return true;
            }
            return false;
#elif URP_AVAILABLE && !UNITY_2023_2_OR_NEWER
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            return urp.scriptableRenderer.GetType().FullName.Contains("Renderer2D");
#else
            return false;
#endif
        }

        /// <summary>
        /// Are we currently using the Legacy Render Pipeline.
        /// </summary>
        /// <returns>bool</returns>
        internal static bool UsingLegacyRP()
        {
            var pipelineAsset = GraphicsSettings.currentRenderPipeline;
            return pipelineAsset == null;
        }

        /// <summary>
        /// Are we currently capturing SubFrames.
        /// </summary>
        /// <returns>bool</returns>
        internal static bool CaptureAccumulation(RecorderSettings settings)
        {
#if HDRP_AVAILABLE
            var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline != null && settings.IsAccumulationSupported())
            {
                IAccumulation accumulation = settings as IAccumulation;
                if (accumulation != null)
                {
                    AccumulationSettings aSettings = accumulation.GetAccumulationSettings();
                    if (aSettings != null)
                        return aSettings.CaptureAccumulation;
                }
            }
#endif
            return false;
        }

        /// <summary>
        /// Returns the color space of the specified graphics format.
        /// </summary>
        /// <param name="format">The graphics format to probe.</param>
        /// <returns></returns>
        internal static ImageRecorderSettings.ColorSpaceType GetColorSpaceType(GraphicsFormat format)
        {
            // All sRGB formats end with "_SRGB"?
            return format.ToString().EndsWith("_SRGB") ? ImageRecorderSettings.ColorSpaceType.sRGB_sRGB : ImageRecorderSettings.ColorSpaceType.Unclamped_linear_sRGB;
        }

        /// <summary>
        /// Returns the Recorder-specific color space matching the Unity color space.
        /// </summary>
        /// <param name="space">The Unity color space to probe</param>
        /// <returns></returns>
        /// <exception cref="InvalidEnumArgumentException">Throws an exception if the enum value is not as expected.</exception>
        internal static ImageRecorderSettings.ColorSpaceType GetColorSpaceType(ColorSpace space)
        {
            switch (space)
            {
                case ColorSpace.Gamma:
                    return ImageRecorderSettings.ColorSpaceType.sRGB_sRGB;
                case ColorSpace.Linear:
                    return ImageRecorderSettings.ColorSpaceType.Unclamped_linear_sRGB;
                default:
                    throw new InvalidEnumArgumentException($"Unexpected color space '{space}'");
            }
        }

        /// <summary>
        /// Returns True if a manual vertical flip is required, False otherwise.
        /// The decision is based on the user's intention as well as the characteristics of the current graphics API
        /// (OpenGL is flipped vertically compared to Metal & DirectX) and the type of capture source.
        /// </summary>
        /// <param name="wantFlippedTexture">True if the user expects a vertically flipped texture, False otherwise.</param>
        /// <param name="captureSource">The input source for the encoder.</param>
        /// <param name="flipForEncoder">True if the encoder requires a flipped image, False otherwise.</param>
        /// <returns></returns>
        internal static bool NeedToActuallyFlip(bool wantFlippedTexture, BaseRenderTextureInput captureSource,
            bool flipForEncoder)
        {
            // We need to take several things into account: what the user expects, whether or not the rendering is made
            // on a GameView source, and whether or not the hardware is OpenGL.
            bool isGameView = captureSource is GameViewInput; // game view is already flipped
            bool isCameraInputLegacyRP = captureSource is CameraInput && UsingLegacyRP(); // legacy RP has vflipped camera input

            // OpenGL causes a flipped image except if:
            // * source is 360 camera
            // * source is RenderTextureInput
            // * source is RenderTextureSampler
            // * source is CameraInput in a URP project
            bool isFlippedBecauseOfOpenGL = !SystemInfo.graphicsUVStartsAtTop &&
                !(captureSource is Camera360Input || captureSource is RenderTextureInput
                    || captureSource is RenderTextureSampler
                    || (captureSource is CameraInput && UsingURP()));

            // The image will already be flipped if:
            // * the input comes from the GameView, OR
            // * the input comes from a TargetCamera in a LRP project, OR
            // * the OpenGL context flips it
            bool willBeFlipped = isGameView ^ flipForEncoder ^ isCameraInputLegacyRP ^ isFlippedBecauseOfOpenGL;

            // We flip if the user's intention is different from the result, and take into account the Y axis convention of the encoder
            return willBeFlipped != wantFlippedTexture;
        }

        /// <summary>
        /// Whether the current number of audio channels is supported by the recorder.
        /// </summary>
        /// <returns>bool</returns>
        internal static bool IsNumAudioChannelsSupported()
        {
            return AudioSettings.speakerMode is AudioSpeakerMode.Mono or AudioSpeakerMode.Stereo;
        }

        /// <summary>
        /// Returns the number of audio channels of the project.
        /// </summary>
        /// <returns>The number of audio channels of the project.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown if the speaker mode is not supported.</exception>
        internal static uint GetNumAudioChannels()
        {
            return GetNumAudioChannels(AudioSettings.speakerMode);
        }

        /// <summary>
        /// Returns the number of audio channels for a given speaker mode.
        /// </summary>
        /// <returns>The number of audio channels of the project.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown if the speaker mode is not supported</exception>
        internal static uint GetNumAudioChannels(AudioSpeakerMode mode)
        {
            switch (mode)
            {
                case AudioSpeakerMode.Mono:
                    return 1;
                case AudioSpeakerMode.Prologic: // not supported, but recognized.
                case AudioSpeakerMode.Stereo:
                    return 2;
                case AudioSpeakerMode.Quad:
                    return 4;
                case AudioSpeakerMode.Surround:
                    return 5;
                case AudioSpeakerMode.Mode5point1:
                    return 6;
                case AudioSpeakerMode.Mode7point1:
                    return 8;
                default:
                    throw new InvalidEnumArgumentException($"Unsupported speaker mode '{AudioSettings.speakerMode}'");
            }
        }

        /// <summary>
        /// Returns the name of a given speaker mode. If no speaker mode is provided, the project's speaker mode
        /// is probed.
        /// </summary>
        /// <returns>The number of audio channels of the project.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown if the speaker mode is not supported</exception>
        internal static string GetSpeakerModeName(AudioSpeakerMode mode)
        {
            switch (mode)
            {
                case AudioSpeakerMode.Mono:
                    return "Mono";
                case AudioSpeakerMode.Prologic:
                    return "Prologic DTS";
                case AudioSpeakerMode.Stereo:
                    return "Stereo";
                case AudioSpeakerMode.Quad:
                    return "Quad";
                case AudioSpeakerMode.Surround:
                    return "Surround";
                case AudioSpeakerMode.Mode5point1:
                    return "Surround 5.1";
                case AudioSpeakerMode.Mode7point1:
                    return "Surround 7.1";
                default:
                    throw new InvalidEnumArgumentException($"Unsupported speaker mode '{AudioSettings.speakerMode}'");
            }
        }

        /// <summary>
        /// Returns error message that is raised when the current default speaker mode is not supported depending on
        /// current encoder and current speaker mode.
        /// </summary>
        ///<param name="encoderName">Current encoder.</param>
        /// ///<param name="supportedSpeakerModes">Speaker modes supported by the encoder.</param>
        /// <returns>Error message.</returns>
        internal static string GetUnsupportedSpeakerModeErrorMessage(string encoderName, AudioSpeakerMode[] supportedSpeakerModes)
        {
            var defaultSpeakerModeName = GetSpeakerModeName(AudioSettings.speakerMode);
            var speakerModesMsg = AudioSpeakerModesToString(supportedSpeakerModes);
            return
                $"The {encoderName} only supports {speakerModesMsg} audio recording. The Default Speaker Mode is {defaultSpeakerModeName}.";
        }

        /// <summary>
        /// Returns an array of AudioSpeakerModes in a human readable string (ex: "speakerMode1, speakerMode2 and speakerMode3")
        /// </summary>
        /// ///<param name="speakerModes">An array of speaker modes</param>
        /// <returns>SpeakerModes separated by commas and 'and' for the last one.</returns>
        internal static string AudioSpeakerModesToString(AudioSpeakerMode[] speakerModes)
        {
            return string.Join(" ", speakerModes.Select((v, i) =>
            {
                if (i < speakerModes.Length - 2)
                    return $"{GetSpeakerModeName(v)},";
                if (i < speakerModes.Length - 1)
                    return $"{GetSpeakerModeName(v)} and";
                return GetSpeakerModeName(v);
            }));
        }
    }
}
