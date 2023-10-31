using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Recorder
{
    [CustomEditor(typeof(AOVRecorderSettings))]
    class AOVRecorderEditor : RecorderEditor
    {
        SerializedProperty m_OutputFormat;
        SerializedProperty m_EXRCompression;
        SerializedProperty m_EXRCompressionLevel;
        SerializedProperty m_ColorSpace;
        SerializedProperty m_IsMultiPartEXR;

        static readonly string[] k_ListOfColorspaces = new[] { "sRGB, sRGB", "Linear, sRGB (unclamped)" };
        static readonly string[] k_ListOfOutputFormats = new[] { "PNG", "EXR" };

        Dictionary<GUIContent, List<AOVType>> m_AovCategories;
        Dictionary<AOVType, GUIContent> m_AovDisplayNames;
        AOVRecorderSettings m_AOVRecorderSettings;
        SerializedProperty m_AOVMultiselection;

        static class Styles
        {
            internal static readonly int windowMaxWidth = 580;
            internal static readonly int columnWidth  = 250;

            internal static readonly GUIContent FormatLabel = new GUIContent("Format");

            internal static readonly GUIContent AOVLabel =
                new GUIContent("Arbitrary Output Variables (AOVs)", "Select one or multiple AOVs to export.");

            internal static readonly GUIContent AOVCLabel = new GUIContent("Compression",
                "The data compression method to apply when using the EXR format.");

            internal static readonly GUIContent AOVMultiPartEXR = new GUIContent("Multi-part file",
                "Export all selected AOVs to a single multi-part EXR file instead of exporting each AOV to a separate EXR file.");

            internal static readonly GUIContent EXRCompressionLevelLabel = new GUIContent("Compression Level",
                "The level of data compression method to apply when using a compression algorithm that supports different levels.");

            internal static readonly GUIContent ColorSpace = new GUIContent("Color Space",
                "The color space (gamma curve, gamut) to use in the output images.\n\nIf you select an option to get unclamped values, you must:\n- Use High Definition Render Pipeline (HDRP).\n- Disable any Tonemapping in your Scene.\n- Disable Dithering on the selected Camera.");

            // AOV LABELS
            internal static readonly GUIContent AlbedoLabel =
                new GUIContent("Albedo",
                    "The overall perceived colors of the surfaces in view, with no lighting calculation and no shadows.");

            internal static readonly GUIContent AlphaLabel =
                new GUIContent("Alpha", "The opacity of the surfaces in view.");

            internal static readonly GUIContent MetalLabel =
                new GUIContent("Metal", "The metallic aspect of the surfaces in view.");

            internal static readonly GUIContent SmoothnessLabel =
                new GUIContent("Smoothness", "The smoothness of the surfaces in view.");

            internal static readonly GUIContent SpecularLabel =
                new GUIContent("Specular", "The specular colors of the surfaces in view.");

            internal static readonly GUIContent DirectDiffuseLabel =
                new GUIContent("Direct Diffuse", "The direct diffuse light response of the surfaces in view.");

            internal static readonly GUIContent DirectSpecularLabel =
                new GUIContent("Direct Specular", "The direct specular light response of the surfaces in view.");

            internal static readonly GUIContent IndirectDiffuseLabel =
                new GUIContent("Indirect Diffuse", "The indirect diffuse light response of the surfaces in view.");

            internal static readonly GUIContent EmissiveLabel =
                new GUIContent("Emissive", "The light emitted by the surfaces in view.");

            internal static readonly GUIContent ReflectionLabel =
                new GUIContent("Reflection ", "The light reflected by the surfaces in view.");

            internal static readonly GUIContent RefractionLabel =
                new GUIContent("Refraction", "The light refracted through the surfaces in view.");

            internal static readonly GUIContent AmbientOcclusionLabel =
                new GUIContent("Ambient Occlusion",
                    "The data resulting from ambient occlusion post-process of the Scene.");

            internal static readonly GUIContent DepthLabel =
                new GUIContent("Depth",
                    "The relative distances of the Scene elements in view between the Far Plane and the Near Plane of the recording Camera.");

            internal static readonly GUIContent MotionVectorsLabel =
                new GUIContent("Motion Vectors",
                    "The 2D vectors representing the movements in the Scene, relative to the recording Camera.");

            internal static readonly GUIContent NormalLabel =
                new GUIContent("Normal",
                    "The data resulting from geometric normals and normal maps of the surfaces in view.");

            // AOV categories labels
            internal static readonly GUIContent BeautyCategoryLabel =
                new GUIContent("Beauty", "The final rendered view after post-process.");

            internal static readonly GUIContent MaterialPropertyCategoryLabel =
                new GUIContent("Material Properties",
                    "Common inherent material properties of the surfaces in view. This includes data from material values or material maps.");

            internal static readonly GUIContent LightingCategoryLabel =
                new GUIContent("Lighting", "All the lighting modes that contribute to the Beauty.");

            internal static readonly GUIContent UtilityCategoryLabel =
                new GUIContent("Utility", "Various data computed from the Scene.");
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (target == null)
                return;

            SetUpAovUtilityDictionaries();

            m_AOVRecorderSettings = (AOVRecorderSettings)target;
            m_AOVMultiselection = serializedObject.FindProperty("m_AOVMultiSelection");
            m_OutputFormat = serializedObject.FindProperty("m_OutputFormat");
            m_EXRCompression = serializedObject.FindProperty("m_EXRCompression");
            m_EXRCompressionLevel = serializedObject.FindProperty("m_EXRCompressionLevel");
            m_ColorSpace = serializedObject.FindProperty("m_ColorSpace");
            m_IsMultiPartEXR = serializedObject.FindProperty("m_IsMultiPartEXR");
        }

        void SetUpAovUtilityDictionaries()
        {
            m_AovDisplayNames = new Dictionary<AOVType, GUIContent>
            {
                { AOVType.Beauty, Styles.BeautyCategoryLabel },
                //material properties
                { AOVType.Albedo, Styles.AlbedoLabel },
                { AOVType.Alpha, Styles.AlphaLabel },
                { AOVType.Metal, Styles.MetalLabel },
                { AOVType.Smoothness, Styles.SmoothnessLabel },
                { AOVType.Specular, Styles.SpecularLabel },
                //lighting
                { AOVType.DirectDiffuse, Styles.DirectDiffuseLabel },
                { AOVType.DirectSpecular, Styles.DirectSpecularLabel },
                { AOVType.Emissive, Styles.EmissiveLabel },
                { AOVType.IndirectDiffuse, Styles.IndirectDiffuseLabel },
                { AOVType.Reflection, Styles.ReflectionLabel },
                { AOVType.Refraction, Styles.RefractionLabel },
                //utility
                { AOVType.AmbientOcclusion, Styles.AmbientOcclusionLabel },
                { AOVType.Depth, Styles.DepthLabel },
                { AOVType.MotionVectors, Styles.MotionVectorsLabel },
                { AOVType.Normal, Styles.NormalLabel },
            };

            m_AovCategories = new Dictionary<GUIContent, List<AOVType>>
            {
                { Styles.BeautyCategoryLabel, new List<AOVType> { AOVType.Beauty } },
                {
                    Styles.MaterialPropertyCategoryLabel,
                    new List<AOVType>
                    { AOVType.Albedo, AOVType.Alpha, AOVType.Metal, AOVType.Smoothness, AOVType.Specular }
                },
                {
                    Styles.LightingCategoryLabel,
                    new List<AOVType>
                    {
                        AOVType.DirectDiffuse, AOVType.DirectSpecular, AOVType.Emissive, AOVType.IndirectDiffuse,
                        AOVType.Reflection, AOVType.Refraction
                    }
                },
                {
                    Styles.UtilityCategoryLabel,
                    new List<AOVType> { AOVType.AmbientOcclusion, AOVType.Depth, AOVType.MotionVectors, AOVType.Normal }
                }
            };
        }

        int OutputFormatEnumToSelectedIdx(int format)
        {
            switch (format)
            {
                case (int)ImageRecorderSettings.ImageRecorderOutputFormat.PNG:
                    return 0;
                case (int)ImageRecorderSettings.ImageRecorderOutputFormat.EXR:
                    return 1;
                default:
                    throw new Exception("Unsupported format for AOV Recorder");
            }
        }

        int SelectedIdxToOutputFormatEnum(int selectedIdx)
        {
            switch (selectedIdx)
            {
                case 0:
                    return (int)ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
                case 1:
                    return (int)ImageRecorderSettings.ImageRecorderOutputFormat.EXR;
                default:
                    throw new Exception("Unsupported format for AOV Recorder");
            }
        }

        protected override void FileTypeAndFormatGUI()
        {
            if (m_OutputFormat.intValue == (int)ImageRecorderSettings.ImageRecorderOutputFormat.JPEG)
            {
                // Give the user the opportunity to upgrade
                m_OutputFormat.intValue = (int)(ImageRecorderSettings.ImageRecorderOutputFormat)EditorGUILayout.EnumPopup(Styles.FormatLabel, ImageRecorderSettings.ImageRecorderOutputFormat.JPEG,
                    val => (ImageRecorderSettings.ImageRecorderOutputFormat)val !=
                    ImageRecorderSettings.ImageRecorderOutputFormat.JPEG, false);
            }
            else
            {
                var selectedIdx = OutputFormatEnumToSelectedIdx(m_OutputFormat.intValue);
                selectedIdx =
                    EditorGUILayout.Popup(Styles.FormatLabel, selectedIdx, k_ListOfOutputFormats);
                m_OutputFormat.intValue = SelectedIdxToOutputFormatEnum(selectedIdx);

                using (new EditorGUI.IndentLevelScope(1))
                {
                    if (m_OutputFormat.intValue == (int)ImageRecorderSettings.ImageRecorderOutputFormat.EXR)
                    {
                        EditorGUILayout.PropertyField(m_IsMultiPartEXR, Styles.AOVMultiPartEXR);
                    }
                    else
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.Toggle(Styles.AOVMultiPartEXR, false);
                        }
                    }
                }
            }

            var imageSettings = (AOVRecorderSettings)target;

            if (imageSettings.CanCaptureHDRFrames())
            {
                m_ColorSpace.intValue =
                    EditorGUILayout.Popup(Styles.ColorSpace, m_ColorSpace.intValue, k_ListOfColorspaces);
            }
            else
            {
                using (new EditorGUI.DisabledScope(!imageSettings.CanCaptureHDRFrames()))
                {
                    EditorGUILayout.Popup(Styles.ColorSpace, 0, k_ListOfColorspaces);
                }
            }

            if ((ImageRecorderSettings.ImageRecorderOutputFormat)m_OutputFormat.enumValueIndex ==
                ImageRecorderSettings.ImageRecorderOutputFormat.EXR)
            {
                EditorGUILayout.PropertyField(m_EXRCompression, Styles.AOVCLabel);
                bool supportsCompressionLevel =
                    CompressionUtility.SupportsCompressionLevel(
                        (CompressionUtility.EXRCompressionType)m_EXRCompression.intValue);

                using (new EditorGUI.DisabledScope(!supportsCompressionLevel))
                {
                    if (supportsCompressionLevel)
                    {
                        // Only DWAA or DWAB support compression levels
                        m_EXRCompressionLevel.intValue = EditorGUILayout.IntField(Styles.EXRCompressionLevelLabel,
                            m_EXRCompressionLevel.intValue);
                        m_EXRCompressionLevel.intValue =
                            Math.Max(
                                Math.Min(m_EXRCompressionLevel.intValue,
                                    CompressionUtility.DWACompressionTypeInfo.k_MaxValue),
                                CompressionUtility.DWACompressionTypeInfo.k_MinValue);
                    }
                    else
                    {
                        EditorGUILayout.IntField(Styles.EXRCompressionLevelLabel, 0);
                    }
                }
            }
        }

        private void OnSelected(AOVType selectedIdx)
        {
            var aovSettings = (AOVRecorderSettings)target;
            Undo.RegisterCompleteObjectUndo(aovSettings, "Select AOV");
            var list = aovSettings.GetAOVSelection().ToHashSet();
            if (list.Contains(selectedIdx))
            {
                list.Remove(selectedIdx);
            }
            else
            {
                list.Add(selectedIdx);
            }

            aovSettings.SetAOVSelection(list.ToArray());
            EditorUtility.SetDirty(aovSettings);
        }

        internal override void CustomGUI()
        {
            base.CustomGUI();
#if HDRP_AVAILABLE

            serializedObject.Update();

            EditorGUILayout.LabelField(Styles.AOVLabel);

            foreach (KeyValuePair<GUIContent, List<AOVType>> group in m_AovCategories)
            {
                DrawAovCategoryTitleToggle(group);

                EditorGUILayout.Space(2.0f);

                // Do not redraw Beauty AOV
                if (group.Value.Count != 1)
                {
                    var numColumns = EditorGUIUtility.currentViewWidth < Styles.windowMaxWidth ? 1 : 2;
                    DrawAovCategoryToggle(group, numColumns);
                    EditorGUILayout.Space(-3f);
                }
            }

            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
#else
            // Draw nothing, user will see errors (see AOVRecorderSettings.IsInvalid) and an empty AOV GUI
#endif
        }

        void DrawAovCategoryTitleToggle(KeyValuePair<GUIContent, List<AOVType>> category)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // Add some space to the left of the category's toggle
                using (new EditorGUILayout.HorizontalScope(GUILayout.Width(20f)))
                {
                    EditorGUILayout.Space();
                }

                var mixedValue = IsCategorySelectionMixed(category.Value);

                if (mixedValue)
                    EditorGUI.showMixedValue = true;

                var isCategorySelected =
                    category.Value.All(aov => m_AOVRecorderSettings.GetAOVSelection().Contains(aov));

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    var toggleGroup =
                        EditorGUILayout.ToggleLeft(category.Key, isCategorySelected, GUILayout.ExpandWidth(true));

                    if (mixedValue)
                        EditorGUI.showMixedValue = false;

                    if (check.changed)
                    {
                        if (!mixedValue || toggleGroup)
                        {
                            //Set all AOVs in the category to the same value as the category's toggle
                            foreach (var aov in category.Value)
                            {
                                ApplySelectionChanges(toggleGroup, aov);
                            }
                        }
                    }
                }
            }
        }

        void DrawAovCategoryToggle(KeyValuePair<GUIContent, List<AOVType>> category, int numColumns)
        {
            var numRows = Mathf.CeilToInt(category.Value.Count / (float)numColumns);

            using (new EditorGUILayout.HorizontalScope())
            {
                for (var columnIndex = 0; columnIndex < numColumns; columnIndex++)
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(1.5f * Styles.columnWidth)))
                    {
                        for (var rowIndex = 0; rowIndex < numRows; rowIndex++)
                        {
                            var itemIndex = columnIndex * numRows + rowIndex;

                            if (itemIndex >= category.Value.Count)
                                continue;

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                // Add extra space to the left of the checkboxes of the first column to align them with the other elements
                                if (columnIndex == 0)
                                    GUILayout.Space(EditorGUIUtility.labelWidth + 6.0f);

                                var aov = category.Value[itemIndex];

                                using (var check = new EditorGUI.ChangeCheckScope())
                                {
                                    var toggle = EditorGUILayout.ToggleLeft(m_AovDisplayNames[aov],
                                        m_AOVRecorderSettings.GetAOVSelection().Contains(aov),
                                        GUILayout.Width(Styles.columnWidth / 2f), GUILayout.ExpandWidth(true));

                                    if (check.changed)
                                        ApplySelectionChanges(toggle, aov);
                                }
                            }
                        }
                    }
                }
            }
        }

        void ApplySelectionChanges(bool isSelected, AOVType aov)
        {
            if (isSelected)
            {
                if (!m_AOVRecorderSettings.GetAOVSelection().Contains(aov))
                {
                    // Beauty needs to be the first element due to a limitation of OIIO
                    if (aov == AOVType.Beauty)
                    {
                        m_AOVMultiselection.InsertArrayElementAtIndex(0);
                        m_AOVMultiselection.GetArrayElementAtIndex(0).enumValueIndex = (int)aov;
                    }
                    else
                    {
                        m_AOVMultiselection.InsertArrayElementAtIndex(m_AOVMultiselection.arraySize);
                        m_AOVMultiselection.GetArrayElementAtIndex(m_AOVMultiselection.arraySize - 1).enumValueIndex =
                            (int)aov;
                    }
                }
            }
            else
            {
                for (var i = 0; i < m_AOVMultiselection.arraySize; i++)
                {
                    if (m_AOVMultiselection.GetArrayElementAtIndex(i).enumValueIndex == (int)aov)
                    {
                        m_AOVMultiselection.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
            }
        }

        bool IsCategorySelectionMixed(List<AOVType> group)
        {
            var nbrOfAovSelectedInGroup = group.Count(aov => m_AOVRecorderSettings.GetAOVSelection().Contains(aov));
            return 0 < nbrOfAovSelectedInGroup && nbrOfAovSelectedInGroup < group.Count;
        }
    }
}
