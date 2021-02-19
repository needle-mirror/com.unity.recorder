using System;
using System.ComponentModel;
using System.Linq;
using UnityEditor.Recorder.AOV;
using UnityEngine;

namespace UnityEditor.Recorder
{
    [CustomPropertyDrawer(typeof(EXRCompressionType))]
    class AOVCompressionTypePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();
            var compressionLabels = Enum.GetNames(typeof(EXRCompressionType)).ToArray();
            var newValue = EditorGUI.Popup(position, label.text, property.enumValueIndex, compressionLabels);

            property.enumValueIndex = newValue;

            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }
}
