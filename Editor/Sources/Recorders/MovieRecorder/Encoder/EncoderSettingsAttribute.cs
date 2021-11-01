using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEditor.Media;
using UnityEngine;

namespace UnityEditor.Recorder.Encoder
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class EncoderSettingsAttribute : Attribute
    {
        internal Type EncoderType { get; set; }

        public EncoderSettingsAttribute(Type encoderType)
        {
            EncoderType = encoderType;
        }
    }
}
