using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Recorder
{
    [CustomPropertyDrawer(typeof(OutputPath))]
    class OutputPathDrawer : TargetedPropertyDrawer<OutputPath>
    {
        SerializedProperty m_RootProperty;
        SerializedProperty m_LeafProperty;
        SerializedProperty m_ForceAssetFolder;
        SerializedProperty m_AbsolutePathProperty;

        protected override void Initialize(SerializedProperty property)
        {
            base.Initialize(property);

            m_RootProperty = property.FindPropertyRelative("m_Root");
            m_LeafProperty = property.FindPropertyRelative("m_Leaf");
            m_ForceAssetFolder = property.FindPropertyRelative("m_ForceAssetFolder");
            m_AbsolutePathProperty = property.FindPropertyRelative("m_AbsolutePath");
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialize(property);

            label.tooltip = "The path to the folder where the Recorder saves the output files";
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Keyboard), label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            const float rootWidth = 125.0f;
            const float btnWidth = 30.0f;

            var leafWidth = position.width - rootWidth - btnWidth - 10;
            var rootRect = new Rect(position.x, position.y, rootWidth, position.height);
            var leafRect = new Rect(position.x + rootWidth + 5, position.y, leafWidth, position.height);
            var btnRect = new Rect(position.x + rootWidth + leafWidth + 10, position.y, btnWidth, position.height);

            var pathType = (OutputPath.Root)m_RootProperty.intValue;
            if (target.forceAssetsFolder)
                GUI.Label(rootRect, pathType + " " + Path.DirectorySeparatorChar);
            else
                EditorGUI.PropertyField(rootRect, m_RootProperty, GUIContent.none);

            if (pathType == OutputPath.Root.Absolute)
                EditorGUI.PropertyField(leafRect, m_AbsolutePathProperty, GUIContent.none); // Show the absolute path
            else
                EditorGUI.PropertyField(leafRect, m_LeafProperty, GUIContent.none); // Show the leaf

            var fullPath = OutputPath.GetFullPath((OutputPath.Root)m_RootProperty.intValue, m_LeafProperty.stringValue,
                m_AbsolutePathProperty.stringValue);

            var tooltip = "Select the output location through your file browser";
            var folder = fullPath;

            if (target.forceAssetsFolder)
            {
                tooltip = "Select the output location in Unity Assets through your file browser";
                folder = Application.dataPath;
            }

            var folderPanelBtnClicked = GUI.Button(btnRect, new GUIContent("...", tooltip));

            if (pathType == OutputPath.Root.Absolute && m_AbsolutePathProperty.stringValue == "")
            {
                // Empty absolute path: force absolute path root to the first drive found
                m_AbsolutePathProperty.stringValue = DriveInfo.GetDrives().First().RootDirectory.FullName;
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();

            if (folderPanelBtnClicked)
            {
                var newPath = EditorUtility.OpenFolderPanel("Select output location", folder, "");
                if (!string.IsNullOrEmpty(newPath))
                {
                    if (target.forceAssetsFolder && !newPath.Contains(Application.dataPath))
                        EditorUtility.DisplayDialog("Invalid Path",
                            "Selected path " + newPath + " must be in the Unity Assets directory",
                            "Ok");
                    else
                    {
                        var newValue = OutputPath.FromPath(newPath);
                        m_RootProperty.intValue = (int)newValue.root;
                        if (newValue.root == OutputPath.Root.Absolute)
                            m_AbsolutePathProperty.stringValue = newValue.leaf;
                        else
                            m_LeafProperty.stringValue = newValue.leaf;
                    }
                }

                m_RootProperty.serializedObject.ApplyModifiedProperties();
                m_AbsolutePathProperty.serializedObject.ApplyModifiedProperties();
                m_LeafProperty.serializedObject.ApplyModifiedProperties();

                GUIUtility.ExitGUI();
            }
        }
    }
}
