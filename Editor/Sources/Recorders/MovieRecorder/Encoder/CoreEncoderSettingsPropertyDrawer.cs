using System;
using System.ComponentModel;
using UnityEngine;
using static UnityEditor.Recorder.Encoder.CoreEncoderSettings;

namespace UnityEditor.Recorder.Encoder
{
    [CustomPropertyDrawer(typeof(CoreEncoderSettings))]
    class CoreEncoderSettingsPropertyDrawer : PropertyDrawer
    {
        static class Styles
        {
            internal static readonly GUIContent CodecLabel = new("Codec", "The choice of codec and container.");
            internal static readonly GUIContent QualityLabel = new("Encoding quality", "The choice of encoding quality.");
            internal static readonly GUIContent TargetBitrate = new GUIContent("Target Bitrate", "The bitrate the encoder tries to average throughout the video, in Mbps.");
            // using Iframe notation because keyframe is a specific thing in animation and might cause confusion
            internal static readonly GUIContent GopLabel = new GUIContent("GOP Size", "The interval between two full images (I-frames).");
            internal static readonly GUIContent BFramesLabel = new GUIContent("B-Frames", "The number of bidirectional predicted frames (maximum 2).");
            // Tooltip taken from https://docs.unity3d.com/2021.2/Documentation/ScriptReference/VideoEncodingProfile.html
            internal static readonly GUIContent ProfileLabel = new GUIContent("Encoding Profile", "Each encoder profile defines a different set of capabilities and constraints on which decoders rely.");
            internal static readonly GUIContent KeyframeDistanceLabel = new GUIContent("Keyframe Distance", "The maximum interval between two full images (I-frames).");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0;
        }

        private bool IsOutputCodecSupported(Enum arg)
        {
            var toCheck = (OutputCodec)arg;
            return CoreEncoderSettings.IsCodecSupportedOnThisPlatform(toCheck);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            // Some properties we want to draw
            var codec = property.FindPropertyRelative("codec");
            var encodingQuality = property.FindPropertyRelative("encodingQuality");
            var targetBitRate = property.FindPropertyRelative("targetBitRate");
            var gopSize = property.FindPropertyRelative("gopSize");
            var numConsecutiveBFrames = property.FindPropertyRelative("numConsecutiveBFrames");
            var encodingProfile = property.FindPropertyRelative("encodingProfile");
            var keyframeDistance = property.FindPropertyRelative("keyframeDistance");

            // Display choice of codec, with some options potentially disabled
            codec.intValue = (int)(OutputCodec)EditorGUILayout.EnumPopup(Styles.CodecLabel, (OutputCodec)codec.intValue, IsOutputCodecSupported, true);
            // Display choice of encoding quality
            encodingQuality.intValue = (int)(VideoEncodingQuality)EditorGUILayout.EnumPopup(Styles.QualityLabel, (VideoEncodingQuality)encodingQuality.intValue);

            ++EditorGUI.indentLevel;
            // only if Advanced Settings is enabled
            if (encodingQuality.intValue == (int)VideoEncodingQuality.Custom)
            {
                EditorGUILayout.PropertyField(targetBitRate, Styles.TargetBitrate);
                switch (codec.intValue)
                {
                    case (int)OutputCodec.MP4: // H.264 format
                        EditorGUILayout.PropertyField(gopSize, Styles.GopLabel);
                        encodingProfile.intValue = (int)(H264EncodingProfile)EditorGUILayout.EnumPopup(Styles.ProfileLabel, (H264EncodingProfile)encodingProfile.intValue);

                        // if using Baseline profile, B-frames are not used
                        if (encodingProfile.intValue != (int)H264EncodingProfile.Baseline)
                            EditorGUILayout.IntSlider(numConsecutiveBFrames, 0, 2, Styles.BFramesLabel);
                        break;
                    case (int)OutputCodec.WEBM:
                        EditorGUILayout.PropertyField(keyframeDistance, Styles.KeyframeDistanceLabel);
                        break;
                    default:
                        throw new InvalidEnumArgumentException($"Unexpected codec '{codec.intValue}'");
                }
            }
            --EditorGUI.indentLevel;

            EditorGUI.EndProperty();
        }
    }
}
