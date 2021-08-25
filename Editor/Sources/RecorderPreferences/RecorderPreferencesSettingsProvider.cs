using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Recorder
{
    class RecorderPreferencesSettingsProvider : SettingsProvider
    {
        const string k_SettingsMenuPath = "Preferences/Recorder";

        static class Contents
        {
            public static readonly GUIContent SettingMenuIcon = EditorGUIUtility.IconContent("_Popup");
            public static readonly GUIContent ResetLabel = EditorGUIUtility.TrTextContent("Reset", "Reset to default.");
        }

        SerializedObject m_SerializedObject;
        SerializedProperty m_VerboseModeProp;
        SerializedProperty m_ShowGOProp;

        public RecorderPreferencesSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords)
            : base(path, scopes, keywords) {}

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_SerializedObject = new SerializedObject(RecorderPreferencesSettings.Instance);
            m_VerboseModeProp = m_SerializedObject.FindProperty("m_VerboseMode");
            m_ShowGOProp = m_SerializedObject.FindProperty("m_ShowGO");
        }

        public override void OnTitleBarGUI()
        {
            m_SerializedObject.Update();

            if (EditorGUILayout.DropdownButton(Contents.SettingMenuIcon, FocusType.Passive, EditorStyles.label))
            {
                var menu = new GenericMenu();
                // Add a reset button
                menu.AddItem(Contents.ResetLabel, false, reset =>
                {
                    RecorderPreferencesSettings.Instance.Reset();
                    RecorderPreferencesSettings.Save();
                }, null);
                menu.ShowAsContext();
            }
        }

        public override void OnGUI(string searchContext)
        {
            m_SerializedObject.Update();

            using (var change = new EditorGUI.ChangeCheckScope())
            using (new RecorderPreferencesWindowGUIScope()) // for styling
            {
                EditorGUILayout.PropertyField(m_VerboseModeProp, new GUIContent("Verbose Mode"));
                EditorGUILayout.PropertyField(m_ShowGOProp, new GUIContent("Show Recorder GameObject"));
                if (change.changed)
                {
                    m_SerializedObject.ApplyModifiedPropertiesWithoutUndo();
                    RecorderPreferencesSettings.Instance.SetPreferences(m_VerboseModeProp.boolValue, m_ShowGOProp.boolValue);
                    RecorderPreferencesSettings.Save();
                }
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new RecorderPreferencesSettingsProvider(
                k_SettingsMenuPath,
                SettingsScope.User,
                GetSearchKeywordsFromSerializedObject(new SerializedObject(RecorderPreferencesSettings.Instance))
            );
        }
    }
}
