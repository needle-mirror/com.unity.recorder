using System;
using UnityEngine;
using static UnityEditor.Recorder.Encoder.ProResEncoderSettings;

namespace UnityEditor.Recorder.Encoder
{
    [CustomPropertyDrawer(typeof(ProResEncoderSettings))]
    class ProResEncoderSettingsPropertyDrawer : PropertyDrawer
    {
        static class Styles
        {
            internal static readonly GUIContent FormatLabel = new("Codec format", "The choice of codec format.");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0;
        }

        private bool IsCodecFormatSupported(Enum arg)
        {
            var toCheck = (OutputFormat)arg;
            return IsOutputFormatSupported(toCheck);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            // Some properties we want to draw
            var format = property.FindPropertyRelative("outputFormat");

            // Display choice of codec format, with some options potentially disabled
            format.intValue = (int)(OutputFormat)EditorGUILayout.EnumPopup(Styles.FormatLabel, (OutputFormat)format.intValue, IsCodecFormatSupported, true);

            EditorGUI.EndProperty();
        }
    }
}
