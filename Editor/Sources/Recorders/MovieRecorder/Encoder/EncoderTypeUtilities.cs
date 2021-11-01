using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.Recorder.Encoder
{
    static class EncoderTypeUtilities
    {
        static Dictionary<Type, Type> encoderSettingsToEncoder;

        public static List<Type> GetEncoderSettings()
        {
            InitializeCache();
            return encoderSettingsToEncoder.Keys.ToList();
        }

        public static IEncoderSettings CreateEncoderSettingsInstance(Type type)
        {
            return (IEncoderSettings)Activator.CreateInstance(type);
        }

        public static IEncoder CreateEncoderInstance(Type settingsType)
        {
            InitializeCache();
            if (encoderSettingsToEncoder.TryGetValue(settingsType, out var encoderType))
            {
                return Activator.CreateInstance(encoderType) as IEncoder;
            }

            throw new Exception($"{settingsType} does not have an associated Encoder.");
        }

        static void InitializeCache()
        {
            if (encoderSettingsToEncoder != null) return;

            encoderSettingsToEncoder = new Dictionary<Type, Type>();

            var encoderSettingsTypes = TypeCache.GetTypesWithAttribute<EncoderSettingsAttribute>().Where(x => typeof(IEncoderSettings).IsAssignableFrom(x)).ToArray();
            foreach (var settingsType in encoderSettingsTypes)
            {
                var attr = settingsType.GetCustomAttributes(typeof(EncoderSettingsAttribute)).First() as EncoderSettingsAttribute;
                if (typeof(IEncoder).IsAssignableFrom(attr.EncoderType))
                {
                    encoderSettingsToEncoder.Add(settingsType, attr.EncoderType);
                }
            }
        }
    }
}
