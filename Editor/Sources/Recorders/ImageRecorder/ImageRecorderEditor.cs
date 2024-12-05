using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Recorder
{
    [CustomEditor(typeof(ImageRecorderSettings))]
    class ImageRecorderEditor : RecorderEditor
    {
        SerializedProperty m_OutputFormat;
        SerializedProperty m_CaptureAlpha;
        SerializedProperty m_ColorSpace;
        SerializedProperty m_EXRCompression;
        SerializedProperty m_JpegQuality;

        static readonly string[] k_ListOfColorspaces = new[] { "sRGB, sRGB", "Linear, sRGB (unclamped)" };
        private static readonly string[] k_ListOfCompressionOptions =
            ((CompressionUtility.EXRCompressionType[])Enum.GetValues(
                typeof(CompressionUtility.EXRCompressionType)))
                .Where(ImageRecorderSettings.IsAvailableForImageSequence) // Get only those available for ImageSequence
                .Select(type => type.ToString()) // Convert to string
                .ToArray();

        static class Styles
        {
            internal static readonly GUIContent FormatLabel = new GUIContent("Media File Format", "The file encoding format of the recorded output.");
            internal static readonly GUIContent CaptureAlphaLabel = new GUIContent("Include Alpha", "Include the alpha channel in the recording.\n\nTo ensure that your project is properly set up for this, refer to 'Recording with alpha' in the Recorder package manual.");
            internal static readonly GUIContent CLabel = new GUIContent("Compression", "The data compression method to apply when using the EXR format.");
            internal static readonly GUIContent JpegQualityLabel = new GUIContent("Quality", "The JPEG encoding quality level.");
            internal static readonly GUIContent ColorSpace = new GUIContent("Color Space", "The color space (gamma curve, gamut) to use in the output images.\n\nIf you select an option to get unclamped values, you must:\n- Use High Definition Render Pipeline (HDRP).\n- Disable any Tonemapping in your Scene.\n- Disable Dithering on the selected Camera.");
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (target == null)
                return;

            m_OutputFormat = serializedObject.FindProperty("outputFormat");
            m_CaptureAlpha = serializedObject.FindProperty("captureAlpha");
            m_EXRCompression = serializedObject.FindProperty("m_EXRCompression");
            m_JpegQuality = serializedObject.FindProperty("m_JpegQuality");
            m_ColorSpace = serializedObject.FindProperty("m_ColorSpace");
        }

        protected override void FileTypeAndFormatGUI()
        {
            EditorGUILayout.PropertyField(m_OutputFormat, Styles.FormatLabel);
            var imageSettings = (ImageRecorderSettings)target;
            using (new EditorGUI.DisabledScope(!imageSettings.CanCaptureAlpha()))
            {
                EditorGUILayout.PropertyField(m_CaptureAlpha, Styles.CaptureAlphaLabel);
            }

            if (imageSettings.CanCaptureHDRFrames())
            {
                m_ColorSpace.intValue =
                    EditorGUILayout.Popup(Styles.ColorSpace, m_ColorSpace.intValue, k_ListOfColorspaces);
            }
            else
            {
                // Disable the dropdown but show sRGB
                using (new EditorGUI.DisabledScope(!imageSettings.CanCaptureHDRFrames()))
                    EditorGUILayout.Popup(Styles.ColorSpace, 0, k_ListOfColorspaces);
            }

            if ((ImageRecorderSettings.ImageRecorderOutputFormat)m_OutputFormat.enumValueIndex ==
                ImageRecorderSettings.ImageRecorderOutputFormat.EXR)
            {
                using (var scope = new EditorGUI.ChangeCheckScope())
                {
                    m_EXRCompression.intValue =
                        EditorGUILayout.Popup(Styles.CLabel, m_EXRCompression.intValue, k_ListOfCompressionOptions);

                    if (scope.changed)
                    {
                        EditorUtility.SetDirty(target);
                    }
                }
            }

            if ((ImageRecorderSettings.ImageRecorderOutputFormat)m_OutputFormat.enumValueIndex ==
                ImageRecorderSettings.ImageRecorderOutputFormat.JPEG)
            {
                EditorGUILayout.IntSlider(m_JpegQuality, 1, 100, Styles.JpegQualityLabel);
            }
        }
    }
}
