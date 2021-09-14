using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Media;
using UnityEditor.Recorder.Input;
using UnityEngine;
using static UnityEditor.Recorder.MovieRecorderSettings;

namespace UnityEditor.Recorder
{
    [CustomEditor(typeof(MovieRecorderSettings))]
    class MovieRecorderEditor : RecorderEditor
    {
        SerializedProperty m_OutputFormat;
        SerializedProperty m_OutputFormatSuffix;
        SerializedProperty m_EncodingQuality;
        SerializedProperty m_CaptureAlpha;
        SerializedProperty m_ContainerFormatSelected;
        SerializedProperty m_EncoderSelected;
        SerializedProperty m_EncoderPresetSelected;
        SerializedProperty m_EncoderPresetSelectedOptions;
        SerializedProperty m_EncoderPresetSelectedName;
        SerializedProperty m_EncoderPresetSelectedSuffixes;
        SerializedProperty m_EncoderColorDefinitionSelected;
        SerializedProperty m_EncoderCustomOptions;
        SerializedProperty m_EncoderOverrideBitRate;
        SerializedProperty m_EncoderOverrideBitRateValue;

        private List<string> EncoderNames
        {
            get
            {
                if (_mEncoderNames != null)
                    return _mEncoderNames;

                _mEncoderNames = new List<string>();
                foreach (var e in RegisteredEncoders)
                {
                    _mEncoderNames.Add(e.GetName());
                }

                return _mEncoderNames;
            }
        }
        private List<string> _mEncoderNames = null;

        private MediaEncoderRegister[] RegisteredEncoders
        {
            get
            {
                if (_mRegisteredEncoders != null)
                    return _mRegisteredEncoders;

                _mRegisteredEncoders = (target as MovieRecorderSettings).encodersRegistered.ToArray();
                return _mRegisteredEncoders;
            }
        }

        private MediaEncoderRegister[] _mRegisteredEncoders = null;


        /// <summary>
        /// Gets the list of supported formats (as strings) for the registered Encoders.
        /// </summary>
        /// <returns></returns>
        private List<VideoRecorderOutputFormat> GetFormatsAvailableInRegisteredEncoders()
        {
            if (_mFormatsAvailableInRegisteredEncoders != null)
                return _mFormatsAvailableInRegisteredEncoders;
            // Look at the formats that are supported by the registered encoders
            _mFormatsAvailableInRegisteredEncoders = new List<VideoRecorderOutputFormat>();
            foreach (var encoder in RegisteredEncoders)
            {
                var currFormats = encoder.GetAvailableFormats();
                // Add to the list of formats for the GUI
                foreach (var format in currFormats)
                    _mFormatsAvailableInRegisteredEncoders.Add(format);
            }

            return _mFormatsAvailableInRegisteredEncoders;
        }

        private List<VideoRecorderOutputFormat> _mFormatsAvailableInRegisteredEncoders = null;

        private List<VideoRecorderOutputFormat> GetFormatsSupportedInRegisteredEncoders()
        {
            if (_mFormatsSupportedInRegisteredEncoders != null)
                return _mFormatsSupportedInRegisteredEncoders;
            // Look at the formats that are supported by the registered encoders
            _mFormatsSupportedInRegisteredEncoders = new List<VideoRecorderOutputFormat>();
            foreach (var encoder in RegisteredEncoders)
            {
                var currFormats = encoder.GetSupportedFormats();
                if (currFormats != null)
                {
                    // Add to the list of formats for the GUI
                    foreach (var format in currFormats)
                        _mFormatsSupportedInRegisteredEncoders.Add(format);
                }
            }

            return _mFormatsSupportedInRegisteredEncoders;
        }

        private List<VideoRecorderOutputFormat> _mFormatsSupportedInRegisteredEncoders = null;

        /// Whether or not we need to show the choices of encoders. This is only enabled if there is at least one
        /// format that is supported by multiple encoders.
        bool needToDisplayEncoderDropDown
        {
            get
            {
                if (_mNeedToDisplayEncoderDropDown != null)
                    return _mNeedToDisplayEncoderDropDown.Value;

                // Determine whether or not any format is supported by more than 1 encoder.
                _mNeedToDisplayEncoderDropDown = false;
                foreach (var v in Enum.GetValues(typeof(VideoRecorderOutputFormat)))
                {
                    var value = (VideoRecorderOutputFormat)v;
                    var supportedCount = 0;
                    foreach (var encoder in RegisteredEncoders)
                    {
                        if (encoder.GetAvailableFormats().Contains(value))
                            supportedCount++;
                    }

                    if (supportedCount > 1)
                    {
                        _mNeedToDisplayEncoderDropDown = true;
                        break;
                    }
                }

                return _mNeedToDisplayEncoderDropDown.Value;
            }
        }

        private bool? _mNeedToDisplayEncoderDropDown = null;

        static class Styles
        {
            internal static readonly GUIContent VideoBitRateLabel = new GUIContent("Quality", "The quality of the output movie.");
            internal static readonly GUIContent FormatLabel = new GUIContent("Media File Format", "The file encoding format of the recorded output.");
            internal static readonly GUIContent CaptureAlphaLabel = new GUIContent("Include Alpha", "To Include the alpha channel in the recording.");
            internal static readonly GUIContent EncoderLabel = new GUIContent("Encoder", "The encoder to choose to generate the output recording");
            internal static readonly GUIContent EncoderCustomOptionsLabel = new GUIContent("Encoder options", "The options available in this encoder");
            internal static readonly GUIContent SourceLabel = new GUIContent("Source", "The input type to use for the recording.");
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (target == null)
                return;

            m_OutputFormat = serializedObject.FindProperty("outputFormat");
            m_OutputFormatSuffix = serializedObject.FindProperty("outputFormatSuffix");
            m_CaptureAlpha = serializedObject.FindProperty("captureAlpha");
            m_EncodingQuality = serializedObject.FindProperty("encodingQuality");
            m_ContainerFormatSelected = serializedObject.FindProperty("containerFormatSelected");
            m_EncoderSelected = serializedObject.FindProperty("encoderSelected");
            m_EncoderPresetSelected = serializedObject.FindProperty("encoderPresetSelected");
            m_EncoderPresetSelectedName = serializedObject.FindProperty("encoderPresetSelectedName");
            m_EncoderPresetSelectedOptions = serializedObject.FindProperty("encoderPresetSelectedOptions");
            m_EncoderPresetSelectedSuffixes = serializedObject.FindProperty("encoderPresetSelectedSuffixes");
            m_EncoderColorDefinitionSelected = serializedObject.FindProperty("encoderColorDefinitionSelected");
            m_EncoderCustomOptions = serializedObject.FindProperty("encoderCustomOptions");
            m_EncoderOverrideBitRate = serializedObject.FindProperty("encoderOverrideBitRate");
            m_EncoderOverrideBitRateValue = serializedObject.FindProperty("encoderOverrideBitRateValue");
        }

        protected override void OnEncodingGui()
        {
        }

        /// <summary>
        /// Indicates whether or not this output format is supported in the current operating system and Unity version.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>True if and only if the output format is supported.</returns>
        private bool IsVideoOutputFormatSupported(Enum arg)
        {
            var toCheck = (VideoRecorderOutputFormat)arg;
            var fmtSupported = GetFormatsSupportedInRegisteredEncoders().Contains(toCheck);
            return fmtSupported;
        }

        protected override void FileTypeAndFormatGUI()
        {
            m_ContainerFormatSelected.intValue = (int)(VideoRecorderOutputFormat)EditorGUILayout.EnumPopup(Styles.FormatLabel, (VideoRecorderOutputFormat)m_ContainerFormatSelected.intValue, IsVideoOutputFormatSupported, true);

            var currentFormatChoice = GetFormatsAvailableInRegisteredEncoders()[m_ContainerFormatSelected.intValue];
            // Get the encoders that support the current format
            var lsEncoderNamesSupportingSelectedFormat = new List<string>();
            int indexLastEncoderForSelectedFormat = 0; // the last encoder supporting the selected format
            // TODO: support for multiple encoders for a given format
            int i = 0;
            foreach (var encoder in RegisteredEncoders)
            {
                var currFormats = encoder.GetAvailableFormats();
                if (currFormats.Contains(currentFormatChoice))
                {
                    lsEncoderNamesSupportingSelectedFormat.Add(encoder.GetName());
                    indexLastEncoderForSelectedFormat = i;
                }

                i++;
            }

            if (needToDisplayEncoderDropDown)
            {
                // Display and save the encoders that support this format
                m_EncoderSelected.intValue = EditorGUILayout.Popup(Styles.EncoderLabel, m_EncoderSelected.intValue, lsEncoderNamesSupportingSelectedFormat.ToArray());
            }
            else
            {
                // Update the choice without showing a drop down. Pick the last (and only) encoder that supports this format
                m_EncoderSelected.intValue = indexLastEncoderForSelectedFormat;
            }

            // Now show all the attributes of the currently selected encoder
            List<IMediaEncoderAttribute> attr = new List<IMediaEncoderAttribute>();
            RegisteredEncoders[m_EncoderSelected.intValue].GetAttributes(out attr);

            // Display popup of codec formats for this encoder
            var movieSettings = target as MovieRecorderSettings;
            // Is the format supported?
            var supported = GetFormatsSupportedInRegisteredEncoders().Contains(currentFormatChoice);
            using (new EditorGUI.DisabledScope(!supported))
            {
                var anAttr = attr.FirstOrDefault(a =>
                    a.GetName() == AttributeLabels[MovieRecorderSettingsAttributes.CodecFormat]);
                if (anAttr != null)
                {
                    MediaPresetAttribute pAttr = (MediaPresetAttribute)anAttr;

                    // Present a popup for the presets (if any) of the selected encoder
                    List<string> presetName = new List<string>();
                    List<string> presetOptions = new List<string>();
                    List<string> presetSuffixes = new List<string>();
                    foreach (var p in pAttr.Value)
                    {
                        presetName.Add(p.displayName);
                        presetOptions.Add(p.options);
                        presetSuffixes.Add(p.suffix);
                    }

                    if (presetName.Count > 0)
                    {
                        ++EditorGUI.indentLevel;
                        m_EncoderPresetSelected.intValue =
                            EditorGUILayout.Popup(pAttr.GetLabel(), m_EncoderPresetSelected.intValue,
                                presetName.ToArray());
                        --EditorGUI.indentLevel;
                        m_EncoderPresetSelectedOptions.stringValue =
                            presetOptions[m_EncoderPresetSelected.intValue];
                        m_EncoderPresetSelectedName.stringValue =
                            presetName[m_EncoderPresetSelected.intValue];
                        m_EncoderPresetSelectedSuffixes.stringValue =
                            presetSuffixes[m_EncoderPresetSelected.intValue];

                        // Save the selected preset value
                        movieSettings.encoderPresetSelected = m_EncoderPresetSelected.intValue;
                        // Display Preset options in the custom field
                        var customEnabled = m_EncoderPresetSelectedName.stringValue == "Custom";
                        if (customEnabled)
                        {
                            m_EncoderCustomOptions.stringValue =
                                EditorGUILayout.TextField(Styles.EncoderCustomOptionsLabel,
                                    m_EncoderCustomOptions.stringValue);
                            movieSettings.encoderCustomOptions = m_EncoderCustomOptions.stringValue;
                        }
                        else
                        {
                            if (presetOptions[movieSettings.encoderPresetSelected].Length != 0)
                            {
                                EditorGUI.indentLevel += 2;
                                EditorGUILayout.SelectableLabel(string.Format("Preset options: {0}",
                                    presetOptions[movieSettings.encoderPresetSelected]));
                                EditorGUI.indentLevel -= 2;
                            }
                        }
                    }
                }

                // Support for color definition in encoder
                anAttr = attr.FirstOrDefault(a =>
                    a.GetName() == AttributeLabels[MovieRecorderSettingsAttributes.ColorDefinition]);
                if (anAttr != null)
                {
                    MediaPresetAttribute pAttr = (MediaPresetAttribute)anAttr;

                    // Present a popup for the color definitions (if any) of the selected encoder
                    var presetName = new List<string>();
                    foreach (var p in pAttr.Value)
                    {
                        presetName.Add(p.displayName);
                    }

                    if (presetName.Count > 0)
                    {
                        ++EditorGUI.indentLevel;
                        m_EncoderColorDefinitionSelected.intValue =
                            EditorGUILayout.Popup(pAttr.GetLabel(), m_EncoderColorDefinitionSelected.intValue,
                                presetName.ToArray());
                        --EditorGUI.indentLevel;

                        // Save the selected preset value
                        movieSettings.encoderColorDefinitionSelected = m_EncoderColorDefinitionSelected.intValue;
                    }
                }

                var showAlphaCheckbox = false;
                if (RegisteredEncoders[m_EncoderSelected.intValue].GetType() == typeof(CoreMediaEncoderRegister))
                {
                    string errorMsg;
                    showAlphaCheckbox = RegisteredEncoders[m_EncoderSelected.intValue]
                        .SupportsTransparency(movieSettings, out errorMsg);
                }
                else if (RegisteredEncoders[m_EncoderSelected.intValue].GetType() == typeof(ProResEncoderRegister))
                {
                    string errorMsg;
                    showAlphaCheckbox = RegisteredEncoders[m_EncoderSelected.intValue]
                        .SupportsTransparency(movieSettings, out errorMsg);
                }
                var format = (VideoRecorderOutputFormat)m_ContainerFormatSelected.intValue;
                if (movieSettings.OutputFormat != format)
                {
                    movieSettings.OutputFormat = format; // force the right format
                    InvokeItemStateRefresh(); // the underlying data has changed after the initial loading, so refresh its state in the Recorder Window
                }

                // Special case for the core media encoder show the encoding bit rate popup
                if (RegisteredEncoders[m_EncoderSelected.intValue].GetType() == typeof(CoreMediaEncoderRegister))
                {
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(m_EncodingQuality, Styles.VideoBitRateLabel);
                    --EditorGUI.indentLevel;
                }

                // Show transparency checkbox
                if (showAlphaCheckbox)
                {
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(m_CaptureAlpha, Styles.CaptureAlphaLabel);
                    --EditorGUI.indentLevel;
                }
            }
        }

        protected override void ImageRenderOptionsGUI()
        {
            var recorder = (RecorderSettings)target;

            foreach (var inputsSetting in recorder.InputsSettings)
            {
                var audioSettings = inputsSetting as AudioInputSettings;
                using (new EditorGUI.DisabledScope(audioSettings != null && UnityHelpers.CaptureAccumulation(recorder)))
                {
                    var p = GetInputSerializedProperty(serializedObject, inputsSetting);
                    EditorGUILayout.PropertyField(p, Styles.SourceLabel);
                }
            }
        }
    }
}
