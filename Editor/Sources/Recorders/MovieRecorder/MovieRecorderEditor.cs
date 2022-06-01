using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.Media;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Recorder.MovieRecorderSettings;

namespace UnityEditor.Recorder
{
    [CustomEditor(typeof(MovieRecorderSettings))]
    class MovieRecorderEditor : RecorderEditor
    {
        SerializedProperty m_EncoderSettings;

        private Rect? lastRect;

        static class Styles
        {
            internal static readonly GUIContent SourceLabel = new GUIContent("Source", "The input type to use for the recording.");
            internal static readonly GUIContent EncoderLabel = new GUIContent("Encoder", "The encoder to use for the recording.");
            internal static readonly GUIContent AlphaLabel = new GUIContent("Include alpha", "Whether or not to include the alpha channel.");
            internal static readonly GUIContent AudioLabel = new GUIContent("Include audio", "Whether or not to include the audio signal.");
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (target == null)
                return;

            m_EncoderSettings = serializedObject.FindProperty("encoderSettings");
        }

        protected override void OnEncodingGui()
        {
        }

        static string GetEncoderDisplayName(Type t)
        {
            var dname = Attribute.GetCustomAttribute(t, typeof(DisplayNameAttribute)) as DisplayNameAttribute;
            if (dname != null && !string.IsNullOrWhiteSpace(dname.DisplayName))
                return dname.DisplayName;
            return t.Name;
        }

        protected override void FileTypeAndFormatGUI()
        {
            var mrs = target as MovieRecorderSettings;
            var encoderTypes = EncoderTypeUtilities.GetEncoderSettings();
            int selectedIdx = 0;
            string[] strings = new string[1];

            if (mrs.EncoderSettings != null)
            {
                selectedIdx = encoderTypes.FindIndex(x => x == mrs.EncoderSettings.GetType());
                strings = encoderTypes.Select(GetEncoderDisplayName).ToArray();
            }
            else
            {
                strings = encoderTypes.Select(GetEncoderDisplayName).Union(new[] {"Invalid Encoder"}).ToArray();
                selectedIdx = strings.Length - 1;
            }

            // Drawing code
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Styles.EncoderLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                if (EditorGUILayout.DropdownButton(new GUIContent(strings[selectedIdx]), FocusType.Passive))
                {
                    var menu = new GenericMenu();
                    for (var k = 0; k < encoderTypes.Count; ++k)
                    {
                        var currType = encoderTypes[k];
                        var encoderInstance = EncoderTypeUtilities.CreateEncoderSettingsInstance(currType);

                        if (encoderInstance.SupportsCurrentPlatform())
                        {
                            menu.AddItem(new GUIContent(strings[k]), k == selectedIdx, (_k) =>
                            {
                                var idx = (int)_k;
                                selectedIdx = idx;
                                var currType = encoderTypes[selectedIdx];
                                Undo.RecordObject(mrs, $"Create New Encoder Settings for Recorder '{mrs.name}', encoder of type '{currType.Name}'");
                                mrs.EncoderSettings = EncoderTypeUtilities.CreateEncoderSettingsInstance(currType);
                                serializedObject.Update();
                                InvokeRecorderDataHasChanged();
                            }, k);
                        }
                        else
                        {
                            // The item is disabled (can't be checked either)
                            menu.AddDisabledItem(new GUIContent(strings[k]));
                        }
                    }

                    if (lastRect.HasValue)
                    {
                        menu.DropDown(lastRect.Value);
                    }
                }

                if (Event.current.type == EventType.Repaint)
                {
                    lastRect = GUILayoutUtility.GetLastRect();
                }
            }

            if (mrs.EncoderSettings == null)
            {
                return;
            }

            // Display selected encoder's fields, greyed out if not supported
            using (new EditorGUI.DisabledScope(!mrs.EncoderSettings.SupportsCurrentPlatform()))
                EditorGUILayout.PropertyField(m_EncoderSettings, true);

            // Expose CaptureAudio and CaptureAlpha from the MovieRecorderSettings but look at input and encoder capabilities
            if (mrs.EncoderSettings.CanCaptureAudio)
                mrs.CaptureAudio = EditorGUILayout.Toggle(Styles.AudioLabel, mrs.CaptureAudio);

            if (!UnityHelpers.UsingURP() && mrs.ImageInputSettings.SupportsTransparent && mrs.EncoderSettings.CanCaptureAlpha)
                mrs.CaptureAlpha = EditorGUILayout.Toggle(Styles.AlphaLabel, mrs.CaptureAlpha);
        }

        protected override void ImageRenderOptionsGUI()
        {
            var recorder = (RecorderSettings)target;

            foreach (var inputsSetting in recorder.InputsSettings)
            {
                var audioSettings = inputsSetting as AudioInputSettings;
                if (audioSettings == null) // don't draw the audio input, let the choice be handled by ExtraOptionsGUI()
                {
                    var p = GetInputSerializedProperty(serializedObject, inputsSetting);
                    EditorGUILayout.PropertyField(p, Styles.SourceLabel);
                }
            }
        }
    }
}
