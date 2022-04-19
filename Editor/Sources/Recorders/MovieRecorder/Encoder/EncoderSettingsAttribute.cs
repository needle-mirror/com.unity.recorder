using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEditor.Media;
using UnityEngine;

namespace UnityEditor.Recorder.Encoder
{
    /// <summary>
    /// An attribute that, when placed on an IEncoderSettings type, can associate with an IEncoder.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class EncoderSettingsAttribute : Attribute
    {
        internal Type EncoderType { get; set; }

        /// <summary>
        /// Constructor for the attribute.
        /// </summary>
        /// <param name="encoderType">The IEncoder type.</param>
        public EncoderSettingsAttribute(Type encoderType)
        {
            EncoderType = encoderType;
        }
    }
}
