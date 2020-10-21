using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace Tests.Editor
{
    class RecorderWindowFixture
    {
        RecorderWindow window;
        readonly List<string> deleteFileList = new List<string>();
        [SetUp]
        public void SetUp()
        {
            window = EditorWindow.GetWindow<RecorderWindow>();
        }

        [Test]
        public void PresetsKeepBindings()
        {
            const string camPath = "Assets/cam.asset";
            const string lightPath = "Assets/light.asset";
            deleteFileList.Add(camPath);
            deleteFileList.Add(lightPath);
            {
                var ars = ScriptableObject.CreateInstance<AnimationRecorderSettings>();
                var ais = ars.InputsSettings.First() as AnimationInputSettings;
                ais.gameObject = GameObject.Find("Main Camera");
                var rcs = ScriptableObject.CreateInstance<RecorderControllerSettings>();
                rcs.AddRecorderSettings(ars);
                RecorderControllerSettingsPreset.SaveAtPath(rcs, camPath);
                Assert.AreEqual("Main Camera", ais.gameObject.name);
            }
            {
                var rcs = ScriptableObject.CreateInstance<RecorderControllerSettings>();
                var ars = ScriptableObject.CreateInstance<AnimationRecorderSettings>();
                var ais = ars.InputsSettings.First() as AnimationInputSettings;
                ais.gameObject = GameObject.Find("Directional Light");
                rcs.AddRecorderSettings(ars);
                RecorderControllerSettingsPreset.SaveAtPath(rcs, lightPath);
                Assert.AreEqual("Directional Light", ais.gameObject.name);
            }
            {
                var preset = AssetDatabase.LoadMainAssetAtPath(camPath) as RecorderControllerSettingsPreset;
                var rcs = ScriptableObject.CreateInstance<RecorderControllerSettings>();
                preset.ApplyTo(rcs);
                var ars = rcs.RecorderSettings.First() as AnimationRecorderSettings;
                var ais = ars.AnimationInputSettings;
                Assert.AreEqual("Main Camera", ais.gameObject.name);
            }
            {
                var preset = AssetDatabase.LoadMainAssetAtPath(lightPath) as RecorderControllerSettingsPreset;
                var rcs = ScriptableObject.CreateInstance<RecorderControllerSettings>();
                preset.ApplyTo(rcs);
                var ars = rcs.RecorderSettings.First() as AnimationRecorderSettings;
                var ais = ars.AnimationInputSettings;
                Assert.AreEqual("Directional Light", ais.gameObject.name);
            }
        }

        [TearDown]
        public void TearDown()
        {
            window.Close();
            foreach (var file in deleteFileList)
            {
                Assert.IsTrue(File.Exists(file));
                File.Delete(file);
                Assert.IsFalse(File.Exists(file));
            }

            deleteFileList.Clear();
        }
    }
}
