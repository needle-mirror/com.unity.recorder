using System;
using UnityEngine;

namespace UnityEditor.Recorder
{
    [CustomPropertyDrawer(typeof(AccumulationSettings))]
    class AccumulationSettingsPropertyDrawer : PropertyDrawer
    {
        static class Styles
        {
            // Accumulation motion blur
            internal static readonly GUIContent CaptureAccumulationLabel = new GUIContent("Accumulation", "Capture multiple sub-frames and accumulate them on each final recorded frame to get a motion blur effect or Path Tracing convergence.\nNote: this option might considerably slow down your recording process as it involves a higher amount of rendering steps.");
            internal static readonly GUIContent ShutterInterval = new GUIContent("Shutter Interval", "The portion of the interval between two subsequent frames in which the shutter opens and closes according to the specified Shutter Profile.");
            internal static readonly GUIContent ShutterProfile = new GUIContent("Shutter Profile", "Defines a response profile to simulate the physical motion of a camera shutter at each frame when capturing sub-frames. Either specify a Range to set up a trapezoid-based profile or select an animation Curve.");
            internal static readonly GUIContent AccumulationSamples = new GUIContent("Samples", "The number of sub-frames to capture and accumulate on each final recorded frame.");
            internal static readonly GUIContent ShutterProfileType = new GUIContent("");
            internal static readonly GUIContent UseSubPixelJitter = new GUIContent("Anti-aliasing", "Enables subpixel jitter anti-aliasing.");
        }

        SerializedProperty m_CaptureAccumulation;
        SerializedProperty m_Samples;
        SerializedProperty m_ShutterInterval;
        SerializedProperty m_ShutterType;
        SerializedProperty m_ShutterProfileCurve;
        SerializedProperty m_ShutterFullyOpen;
        SerializedProperty m_ShutterBeginsClosing;
        SerializedProperty m_UseSubPixelJitter;
        RecorderEditor.SavedBool shutterIntervalInAngle;

        static string[] s_IntervalStrings = { "Normalized", "Angle" };


        void Initialize(SerializedProperty property)
        {
            m_CaptureAccumulation = property.FindPropertyRelative("captureAccumulation");
            m_Samples = property.FindPropertyRelative("samples");
            m_ShutterInterval = property.FindPropertyRelative("shutterInterval");
            m_ShutterType = property.FindPropertyRelative("shutterProfileType");
            m_ShutterProfileCurve = property.FindPropertyRelative("shutterProfileCurve");
            m_ShutterFullyOpen = property.FindPropertyRelative("shutterFullyOpen");
            m_ShutterBeginsClosing = property.FindPropertyRelative("shutterBeginsClosing");
            m_UseSubPixelJitter = property.FindPropertyRelative("useSubPixelJitter");
            shutterIntervalInAngle = new RecorderEditor.SavedBool("RecorderAccumulation.shutterIntervalInAngle", false);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialize(property);

            using (new EditorGUI.PropertyScope(position, label, property))
            {
                EditorGUILayout.PropertyField(m_CaptureAccumulation, Styles.CaptureAccumulationLabel);
                using (new EditorGUI.IndentLevelScope(1))
                {
                    using (new EditorGUI.DisabledScope(!m_CaptureAccumulation.boolValue))
                    {
                        EditorGUILayout.PropertyField(m_Samples, Styles.AccumulationSamples);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.PrefixLabel(Styles.ShutterInterval);
                            using (new EditorGUI.IndentLevelScope(-1))
                            {
                                var selected = shutterIntervalInAngle.value == false ? 0 : 1;
                                shutterIntervalInAngle.value = EditorGUILayout.Popup(selected, s_IntervalStrings,
                                    GUILayout.Width(70)) == 1;
                                if (shutterIntervalInAngle.value)
                                {
                                    m_ShutterInterval.floatValue =
                                        EditorGUILayout.Slider(m_ShutterInterval.floatValue * 360, 0.0f,
                                            360.0f) / 360;
                                }
                                else
                                {
                                    m_ShutterInterval.floatValue =
                                        EditorGUILayout.Slider(m_ShutterInterval.floatValue, 0.0f,
                                            1.0f);
                                }
                            }
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.PrefixLabel(Styles.ShutterProfile);
                            using (new EditorGUI.IndentLevelScope(-1))
                            {
                                EditorGUILayout.PropertyField(m_ShutterType, Styles.ShutterProfileType,
                                    GUILayout.Width(70));

                                if (m_ShutterType.intValue == 1) // curve value
                                {
                                    m_ShutterProfileCurve.animationCurveValue = EditorGUILayout.CurveField(
                                        m_ShutterProfileCurve.animationCurveValue, Color.red,
                                        new Rect(0.0f, 0.0f, 1.0f, 1.0f));
                                }
                                else
                                {
                                    var fullyOpen = m_ShutterFullyOpen.floatValue;
                                    var beginsClosing = m_ShutterBeginsClosing.floatValue;

                                    using (var c = new EditorGUI.ChangeCheckScope())
                                    {
                                        fullyOpen = EditorGUILayout.FloatField(fullyOpen);
                                        fullyOpen = Mathf.Clamp(
                                            fullyOpen, 0.0f,
                                            beginsClosing);
                                        if (c.changed)
                                        {
                                            m_ShutterFullyOpen.floatValue = fullyOpen;
                                        }
                                    }

                                    var minValue = fullyOpen;
                                    var maxValue = beginsClosing;

                                    using (var c = new EditorGUI.ChangeCheckScope())
                                    {
                                        EditorGUILayout.MinMaxSlider(ref minValue, ref maxValue, 0.0f, 1.0f);
                                        minValue = Mathf.Round(minValue * 100) / 100.0f;
                                        maxValue = Mathf.Round(maxValue * 100) / 100.0f;

                                        fullyOpen = minValue;
                                        beginsClosing = maxValue;
                                        beginsClosing =
                                            EditorGUILayout.FloatField(beginsClosing);
                                        beginsClosing = Mathf.Clamp(beginsClosing,
                                            fullyOpen, 1.0f);
                                        if (c.changed)
                                        {
                                            m_ShutterFullyOpen.floatValue = fullyOpen;
                                            m_ShutterBeginsClosing.floatValue = beginsClosing;
                                        }
                                    }
                                }
                            }
                        }

                        EditorGUILayout.PropertyField(m_UseSubPixelJitter, Styles.UseSubPixelJitter);

                        var effectiveNumSamples = Math.Max((int)(m_Samples.intValue * m_ShutterInterval.floatValue), 1);
                        EditorGUILayout.HelpBox($"Effective number of accumulated samples: {effectiveNumSamples}", MessageType.None);
                    }
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0;
        }
    }
}
