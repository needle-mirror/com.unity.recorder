using JetBrains.Annotations;

namespace UnityEditor.Recorder
{
    /// <summary>
    /// Options class for the Recorder
    /// </summary>
    public static class RecorderOptions
    {
        const string k_ShowLegacyModeMenuItem = RecorderWindow.MenuRoot + "Options/Show Legacy Recorders";
        const string k_RecorderPanelWidth = RecorderWindow.MenuRoot + "Options/Recorder Panel Width";
        const string k_SelectedRecorderIndex = RecorderWindow.MenuRoot + "Options/Selected Recorder Index";

        /// <summary>
        /// If true, the recorder will log additional recording steps into the Console.
        /// </summary>
        public static bool VerboseMode
        {
            get => RecorderPreferencesSettings.Instance.VerboseMode;
            set => RecorderPreferencesSettings.Instance.VerboseMode = value;
        }

        /// <summary>
        /// The recorder uses a "Unity-RecorderSessions" GameObject to store Scene references and manage recording sessions.
        /// If true, this GameObject will be visible in the Scene Hierarchy.
        /// </summary>
        public static bool ShowRecorderGameObject
        {
            get => RecorderPreferencesSettings.Instance.ShowGO;
            set
            {
                RecorderPreferencesSettings.Instance.ShowGO = value;
            }
        }

        internal static float recorderPanelWidth
        {
            get { return EditorPrefs.GetFloat(k_RecorderPanelWidth, 0); }
            set { EditorPrefs.SetFloat(k_RecorderPanelWidth, value); }
        }

        internal static int selectedRecorderIndex
        {
            get { return EditorPrefs.GetInt(k_SelectedRecorderIndex, 0); }
            set { EditorPrefs.SetInt(k_SelectedRecorderIndex, value); }
        }
    }

    [UsedImplicitly]
    static class Options
    {
        // This variable is used to select how we capture the final image from the
        // render pipeline, with the legacy render pipeline this variable is set to false
        // with the scriptable render pipeline the CameraCaptureBride
        // inside the SRP will reflection to set this variable to true, this will in turn
        // enable using the CameraInput inputStrategy CaptureCallbackInputStrategy
        //
        // This variable is set through reflection by SRP. Everything is matching very strictly: all flags are mandatory as well as the name.
        public static bool useCameraCaptureCallbacks = false;
    }
}
