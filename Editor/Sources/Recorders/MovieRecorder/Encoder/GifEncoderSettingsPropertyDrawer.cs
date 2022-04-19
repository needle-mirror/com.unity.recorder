using System;
using UnityEngine;
using static UnityEditor.Recorder.Encoder.GifEncoderSettings;

namespace UnityEditor.Recorder.Encoder
{
    [CustomPropertyDrawer(typeof(GifEncoderSettings))]
    class GifEncoderSettingsPropertyDrawer : PropertyDrawer
    {
        static class Styles
        {
            internal static readonly GUIContent QualityLabel = new("Quality", "The encoding quality of the GIF file. A higher quality results in a larger file size.");
            internal static readonly GUIContent LoopLabel = new("Loop", "Makes the generated file loop the frame sequence indefinitely.");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            // Some properties we want to draw
            var loop = property.FindPropertyRelative("loop");
            var quality = property.FindPropertyRelative("quality");

            // Display choice of codec format, with some options potentially disabled
            quality.intValue = EditorGUILayout.IntSlider(Styles.QualityLabel, quality.intValue, 1, 100);
            loop.boolValue = EditorGUILayout.Toggle(Styles.LoopLabel, loop.boolValue);

            EditorGUI.EndProperty();
        }
    }
}
