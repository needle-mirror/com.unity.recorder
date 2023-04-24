using System.Collections.Generic;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace UnityEditor.Recorder
{
    /// <summary>
    /// A class that represents the settings of an Audio Recorder.
    /// </summary>
    [RecorderSettings(typeof(AudioRecorder), "Audio")]
    public class AudioRecorderSettings : RecorderSettings
    {
        [SerializeField] AudioInputSettings m_AudioInputSettings = new AudioInputSettings();
        internal readonly AudioSpeakerMode[] kSupportedSpeakerModes = new AudioSpeakerMode[] { AudioSpeakerMode.Mono , AudioSpeakerMode.Stereo};

        protected internal override string Extension
        {
            get { return "wav"; }
        }

        AudioInputSettings AudioInputSettings
        {
            get { return m_AudioInputSettings; }
        }

        /// <inheritdoc/>
        public override IEnumerable<RecorderInputSettings> InputsSettings
        {
            get { yield return m_AudioInputSettings; }
        }

        /// <inheritdoc/>
        protected internal override void GetErrors(List<string> errors)
        {
            base.GetErrors(errors);
            if (!UnityHelpers.IsNumAudioChannelsSupported())
                errors.Add(UnityHelpers.GetUnsupportedSpeakerModeErrorMessage("Audio Encoder", kSupportedSpeakerModes));
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AudioRecorderSettings()
        {
            fileNameGenerator.FileName = DefaultWildcard.Recorder + "_" + DefaultWildcard.Take;
        }
    }
}
