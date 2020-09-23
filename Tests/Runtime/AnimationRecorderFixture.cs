using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.Recorder.Timeline;
using UnityEngine.TestTools;

namespace UnityEngine.Recorder.Tests
{
    class AnimationRecorderFixture : TimelineFixture
    {
        AnimationRecorderSettings aniSettings;
        [SetUp]
        public void SetUp()
        {
            var recorderAsset = recorderClip.asset as RecorderClip;
            aniSettings =  ScriptableObject.CreateInstance<AnimationRecorderSettings>();
            recorderAsset.settings = aniSettings;

            var input = aniSettings.InputsSettings.First() as AnimationInputSettings;
            input.gameObject = cube;
            input.AddComponentToRecord(typeof(Transform));
            recorderAsset.settings.OutputFile = "Assets/" + Path.GetFileNameWithoutExtension(Path.GetTempFileName());
            deleteFileList.Add(recorderAsset.settings.OutputFile + ".anim");
        }

#if UNITY_2019_3_OR_NEWER
        [UnityTest]
        public IEnumerator TestAggressiveCurveSimplification()
        {
            var input = aniSettings.InputsSettings.First() as AnimationInputSettings;
            input.SimplyCurves = AnimationInputSettings.CurveSimplificationOptions.Lossy;
            director.Play();
            while (director.time < recorderClip.end)
                yield return null;
            AssetDatabase.Refresh();
            var asset = AssetDatabase.LoadAssetAtPath<AnimationClip>(aniSettings.OutputFile + ".anim");
            foreach (var binding in  AnimationUtility.GetCurveBindings(asset))
            {
                var curve = AnimationUtility.GetEditorCurve(asset, binding);
                if (binding.propertyName.Contains("m_LocalRotation") || binding.propertyName.Contains("m_LocalScale"))  // no animation
                {
                    Assert.AreEqual(2, curve.keys.Length);
                    continue;
                }

                if (binding.propertyName.Contains("m_LocalRotation")) // animated
                {
                    Assert.AreEqual(5, curve.keys.Length);
                }
            }
        }

        [UnityTest]
        public IEnumerator TestCurveRegularSimplification()
        {
            var input = aniSettings.InputsSettings.First() as AnimationInputSettings;
            input.SimplyCurves = AnimationInputSettings.CurveSimplificationOptions.Lossless;
            director.Play();
            while (director.time < recorderClip.end)
                yield return null;
            AssetDatabase.Refresh();
            var asset = AssetDatabase.LoadAssetAtPath<AnimationClip>(aniSettings.OutputFile + ".anim");
            foreach (var binding in  AnimationUtility.GetCurveBindings(asset))
            {
                var curve = AnimationUtility.GetEditorCurve(asset, binding);
                if (binding.propertyName.Contains("m_LocalRotation") || binding.propertyName.Contains("m_LocalScale"))  // no animation
                {
                    Assert.AreEqual(2, curve.keys.Length);
                    continue;
                }

                if (binding.propertyName.Contains("m_LocalRotation")) // animated
                {
                    Assert.IsTrue(5 < curve.keys.Length);
                }
            }
        }

        [UnityTest]
        public IEnumerator TestDisabledCurveSimplification()
        {
            var input = aniSettings.InputsSettings.First() as AnimationInputSettings;
            input.SimplyCurves = AnimationInputSettings.CurveSimplificationOptions.Disabled;
            director.Play();
            while (director.time < recorderClip.end)
                yield return null;
            AssetDatabase.Refresh();
            var asset = AssetDatabase.LoadAssetAtPath<AnimationClip>(aniSettings.OutputFile + ".anim");
            foreach (var binding in  AnimationUtility.GetCurveBindings(asset))
            {
                var curve = AnimationUtility.GetEditorCurve(asset, binding);
                Assert.IsTrue(5 < curve.keys.Length);
            }
        }

#endif
    }
}
