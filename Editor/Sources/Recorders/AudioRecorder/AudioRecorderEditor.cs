using UnityEngine;

namespace UnityEditor.Recorder
{
    [CustomEditor(typeof(AudioRecorderSettings))]
    class AudioRecorderEditor : RecorderEditor
    {
        static class Styles
        {
            internal static readonly GUIContent FormatLabel = new GUIContent("Format");
        }

        protected override void FileTypeAndFormatGUI()
        {
            EditorGUILayout.Popup(Styles.FormatLabel, 0, new[] { "WAV" });
        }

        protected override void ImageRenderOptionsGUI()
        {
        }

        internal override bool DrawCaptureSection()
        {
            return false;
        }
    }
}
