using System;
using UnityEditor.Presets;
using UnityEngine;

namespace UnityEditor.Recorder
{
    static class PresetHelper
    {
        static Texture2D s_PresetIcon;
        static GUIStyle s_PresetButtonStyle;

        internal static Texture2D presetIcon
        {
            get
            {
                if (s_PresetIcon == null)
                    s_PresetIcon = (Texture2D)EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? "d_Preset.Context@2x" : "Preset@2x.Context");

                return s_PresetIcon;
            }
        }

        internal static GUIStyle presetButtonStyle
        {
            get
            {
                return s_PresetButtonStyle ?? (s_PresetButtonStyle = new GUIStyle("iconButton") { fixedWidth = 19.0f });
            }
        }

        internal static void ShowPresetSelectorWrapper(RecorderSettings settings, Preset currentSelection = null,
            Action onSelectionChanged = null, Action onSelectionClosed = null)
        {
            Action<Preset> OnSelectionChangedIgnoreParams = _ =>
            {
                onSelectionChanged?.Invoke();
            };

            Action<Preset, bool> OnSelectionClosedIgnoreParams = (_, _) =>
            {
                onSelectionClosed?.Invoke();
            };

            PresetSelector.ShowSelector(new UnityEngine.Object[] { settings }, currentSelection, true, OnSelectionChangedIgnoreParams, OnSelectionClosedIgnoreParams);
        }
    }
}
