using UnityEditor.Recorder;
using UnityEngine;

namespace UnityEditor.Recorder
{
    [SettingFilePath("UserSettings/Recorder/RecorderPreferences.asset", SettingFilePathAttribute.Location.PreferencesFolder)]
    class RecorderPreferencesSettings : SettingAsset<RecorderPreferencesSettings>
    {
        [Header("Troubleshooting")]
        [SerializeField, Tooltip("Log extended information about recordings in the Console window.")]
        bool m_VerboseMode;

        [SerializeField, Tooltip("Show the temporary Recorder GameObject for Scene hooks in the Hierarchy during the recording.")]
        bool m_ShowGO;


        /// <summary>
        /// Use this property to log extended information about recordings in the Console window, for troubleshooting purposes.
        /// </summary>
        public bool VerboseMode
        {
            get => m_VerboseMode;
            set => m_VerboseMode = value;
        }

        /// <summary>
        /// Use this property to show the Recorder GameObject for Scene hooks in the Hierarchy during the recording, for troubleshooting purposes.
        /// </summary>
        public bool ShowGO
        {
            get => m_ShowGO;
            set
            {
                m_ShowGO = value;
                UnityHelpers.SetGameObjectsVisibility(value);
            }
        }

        public void SetPreferences(bool verboseMode, bool showGo)
        {
            VerboseMode = verboseMode;
            ShowGO = showGo;
        }

        /// <summary>
        /// Resets the settings to the default values.
        /// </summary>
        public void Reset()
        {
            ShowGO = false;
            VerboseMode = false;
        }
    }
}
