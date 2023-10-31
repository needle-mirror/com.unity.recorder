using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace UnityEditor.Recorder
{
    /// <summary>
    /// The user interface to set up a Recorder.
    /// </summary>
    public abstract class RecorderEditor : Editor
    {
        SerializedProperty m_CaptureEveryNthFrame;
        SerializedProperty m_FileNameGenerator;
        SerializedProperty m_Take;

        internal event Action OnRecorderValidated;
        internal event Action OnRecorderDataHasChaged;
        SavedBool showOutputFormat;
        SavedBool showOutputFile;
        SavedBool showInput;

        // Used to differentiate if drawing from timeline or RecorderWindow
        internal static bool FromRecorderWindow = true;

        static class Styles
        {
            internal static readonly GUIContent InputLabel = new GUIContent("Input");
            internal static readonly GUIContent OutputFormatLabel = new GUIContent("Output Format");
            internal static readonly GUIContent OutputFileLabel = new GUIContent("Output File");
            internal static readonly GUIContent FileNameLabel = new GUIContent("File Name", "Pattern for the name of the output files. It can include a mix of regular text and dynamic placeholders (use the “+ Wildcards” button).");
            internal static readonly GUIContent SourceLabel = new GUIContent("Source", "The input type to use for the recording.");
            internal static readonly GUIContent TakeNumberLabel = new GUIContent("Take Number", "Value that the Recorder uses to number the recordings. It increases by one after each recording.");
            internal static readonly GUIContent RenderStepFrameLabel = new GUIContent("Render Frame Step", "The interval between every frame to render in Play mode during the recording.");
        }


        /// <summary>
        /// Initializes the attributes and styling of the editor based on the serialized data, when the object becomes active.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (target != null)
            {
                var pf = new PropertyFinder<RecorderSettings>(serializedObject);
                m_CaptureEveryNthFrame = pf.Find(w => w.captureEveryNthFrame);
                m_FileNameGenerator = pf.Find(w => w.fileNameGenerator);
                m_Take = pf.Find(w => w.take);

                showOutputFormat = new SavedBool($"{target.GetType()}.showOutputFormat", true);
                showOutputFile = new SavedBool($"{target.GetType()}.showOutputFile", true);;
                showInput = new SavedBool($"{target.GetType()}.showInput", true);;
            }
        }

        /// <summary>
        /// Draws a delimiter between different GUI elements.
        /// </summary>
        protected static void DrawSeparator()
        {
        }

        /// <summary>Draws a foldout header.</summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="state"> The state of the header </param>
        /// <param name="isBoxed"> Specifies whether to contain the header in a box style [optional]. </param>
        /// <returns>return the state of the foldout header</returns>
        static bool DrawHeaderFoldout(GUIContent title, SavedBool state, bool isBoxed = false)
        {
            const float height = 17f;
            var backgroundRect = GUILayoutUtility.GetRect(1f, height);

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;
            foldoutRect.x = labelRect.xMin + 15 * (EditorGUI.indentLevel - 1); //fix for preset

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            if (isBoxed)
            {
                labelRect.xMin += 5;
                foldoutRect.xMin += 5;
                backgroundRect.xMin = EditorGUIUtility.singleLineHeight;
                backgroundRect.width -= 1;
            }

            var lineRect = new Rect(backgroundRect);
            lineRect.height = EditorGUIUtility.standardVerticalSpacing;
            float lineTint =  EditorGUIUtility.isProSkin ? 0.01f : 1f;

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            //line
            EditorGUI.DrawRect(lineRect, new Color(lineTint, lineTint, lineTint, 0.2f));

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Active checkbox
            state.value = GUI.Toggle(foldoutRect, state.value, GUIContent.none, EditorStyles.foldout);

            var e = Event.current;
            if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition)  && e.button == 0)
            {
                state.value = !state;
                e.Use();
            }

            return state;
        }

        /// <summary>
        /// This is the main entry point for drawing the Recorder editor in the inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (target == null)
                return;

            EditorGUI.BeginChangeCheck();
            serializedObject.Update();
            if (DrawCaptureSection())
            {
                showInput.value = DrawHeaderFoldout(Styles.InputLabel, showInput, false);
                if (showInput)
                {
                    EditorGUILayout.Separator();
                    CustomGUI();
                    ImageRenderOptionsGUI();
                    ExtraOptionsGUI();
                    if (UnityHelpers.UsingHDRP())
                    {
                        AccumulationGUI();
                    }
                    EditorGUILayout.Separator();
                }
            }

            showOutputFormat.value = DrawHeaderFoldout(Styles.OutputFormatLabel, showOutputFormat, false);
            if (showOutputFormat)
            {
                EditorGUILayout.Separator();
                FileTypeAndFormatGUI();
                OnEncodingGui();
                EditorGUILayout.Separator();
            }

            showOutputFile.value = DrawHeaderFoldout(Styles.OutputFileLabel, showOutputFile, false);
            if (showOutputFile)
            {
                EditorGUILayout.Separator();
                NameAndPathGUI();
                EditorGUILayout.Separator();
            }

            EditorGUILayout.Separator();
            serializedObject.ApplyModifiedProperties();

            EditorGUI.EndChangeCheck();

            OnValidateSettingsGUI();
        }

        /// <summary>
        /// Validates the settings of the Recorder.
        /// </summary>
        protected virtual void OnValidateSettingsGUI()
        {
            var oldWarnings = new List<string>();
            var targetSettings = target as RecorderSettings;
#pragma warning disable 618
            if (!targetSettings.ValidityCheck(oldWarnings))
#pragma warning restore 618
            {
                foreach (var e in oldWarnings)
                    EditorGUILayout.HelpBox(e, MessageType.Warning);
            }

            var warnings = new List<string>();
            var errors = new List<string>();

            targetSettings.GetWarnings(warnings);
            foreach (var w in warnings)
                EditorGUILayout.HelpBox(w, MessageType.Warning);

            targetSettings.GetErrors(errors);
            foreach (var e in errors)
                EditorGUILayout.HelpBox(e, MessageType.Error);

            if (oldWarnings.Count > 0 || warnings.Count > 0 || errors.Count > 0)
                InvokeRecorderValidated();
        }

        internal void InvokeRecorderValidated()
        {
            OnRecorderValidated?.Invoke();
        }

        internal void InvokeRecorderDataHasChanged()
        {
            OnRecorderDataHasChaged?.Invoke();
        }

        internal virtual bool DrawCaptureSection()
        {
            return true;
        }

        /// <summary>
        /// Displays the information related to the output file(s).
        /// </summary>
        protected virtual void NameAndPathGUI()
        {
            EditorGUILayout.PropertyField(m_FileNameGenerator, Styles.FileNameLabel);

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_Take, Styles.TakeNumberLabel);
        }

        /// <summary>
        /// Displays the information related to the source of the capture and its rendering options.
        /// </summary>
        protected virtual void ImageRenderOptionsGUI()
        {
            var recorder = (RecorderSettings)target;

            foreach (var inputsSetting in recorder.InputsSettings)
            {
                var p = GetInputSerializedProperty(serializedObject, inputsSetting);
                EditorGUILayout.PropertyField(p, Styles.SourceLabel);
            }
        }

        internal static SerializedProperty GetInputSerializedProperty(SerializedObject owner, object fieldValue)
        {
            var targetObject = (object)owner.targetObject;
            var type = targetObject.GetType();

            foreach (var info in InputSettingsSelector.GetInputFields(type))
            {
                if (info.GetValue(targetObject) == fieldValue)
                {
                    return owner.FindProperty(info.Name);
                }

                if (typeof(InputSettingsSelector).IsAssignableFrom(info.FieldType))
                {
                    var selector = info.GetValue(targetObject);
                    var fields = InputSettingsSelector.GetInputFields(selector.GetType());
                    var selectorInput = fields.FirstOrDefault(i => i.GetValue(selector) == fieldValue);

                    if (selectorInput != null)
                    {
                        return owner.FindProperty(info.Name);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Displays extra options below the capture information, under certain conditions.
        /// </summary>
        protected virtual void ExtraOptionsGUI()
        {
            if (((RecorderSettings)target).FrameRatePlayback == FrameRatePlayback.Variable)
            {
                EditorGUILayout.PropertyField(m_CaptureEveryNthFrame, Styles.RenderStepFrameLabel);
            }
        }

        /// <summary>
        /// Displays options related to the output format of the recording.
        /// </summary>
        protected virtual void FileTypeAndFormatGUI()
        {
        }

        /// <summary>
        /// Displays properties related to the encoding of the file.
        /// </summary>
        protected virtual void OnEncodingGui()
        {
        }

        /// <summary>
        /// Override this function to add custom UI at the top of the Input section.
        /// </summary>
        internal virtual void CustomGUI()
        {
        }

        /// <summary>
        /// Displays the information related to the capture of multiframe accumulation.
        /// </summary>
        protected virtual void AccumulationGUI()
        {
            var settings = (RecorderSettings)target;

            if (settings.IsAccumulationSupported())
            {
                var prop = serializedObject.FindProperty("_accumulationSettings");
                if (prop != null)
                    EditorGUILayout.PropertyField(prop);
            }
        }

        internal class SavedBool
        {
            bool m_Value;
            string m_Name;
            bool m_Loaded;
            public SavedBool(string name, bool value)
            {
                m_Name = name;
                m_Loaded = false;
                m_Value = value;
            }

            private void Load()
            {
                if (m_Loaded)
                    return;
                m_Loaded = true;
                m_Value = EditorPrefs.GetBool(m_Name, m_Value);
            }

            public bool value
            {
                get { Load(); return m_Value; }
                set
                {
                    Load();
                    if (m_Value == value)
                        return;
                    m_Value = value;
                    EditorPrefs.SetBool(m_Name, value);
                }
            }
            public static implicit operator bool(SavedBool s)
            {
                return s.value;
            }
        }
    }
}
