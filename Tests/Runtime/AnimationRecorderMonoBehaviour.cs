using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Recorder.Input;
using UnityEngine.Playables;
using UnityEngine.TestTools;
using UnityEngine.Timeline;

namespace UnityEngine.Recorder.Tests
{
    class AnimationRecorderMonoBehaviour : AnimationRecorderFixture
    {
        PlayableDirector recordedDirector;

        [SetUp]
        public new void SetUp()
        {
            var monoDataTimeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("RecordedMonoBehaviour")[0]));
            var go = new GameObject("Timeline");
            recordedDirector = go.AddComponent<PlayableDirector>();
            recordedDirector.playableAsset = monoDataTimeline;
            var track = monoDataTimeline.GetOutputTrack(0);

            cube.AddComponent<RecordableMonoBehaviour>();
            recordedDirector.SetGenericBinding(track, cube.GetComponent<Animator>());
            var input = aniSettings.InputsSettings.First() as AnimationInputSettings;

            input.AddComponentToRecord(typeof(RecordableMonoBehaviour));
        }

        // Test is a bit weak: It tests that all the relevant data is being recorded (it moves).
        // To enforce proper timing and data for the recorder, we'd need a newer timeline with public recording API
        [UnityTest]
        public IEnumerator TestMonoBehaviourRecording()
        {
            director.Play();
            recordedDirector.Play();
            while (director.time < recorderClip.end)
                yield return null;
            AssetDatabase.Refresh();
            var asset = AssetDatabase.LoadAssetAtPath<AnimationClip>(aniSettings.OutputFile + ".anim");
            foreach (var binding in AnimationUtility.GetCurveBindings(asset))
            {
                // This test checks only for MB
                if (binding.type == typeof(RecordableMonoBehaviour))
                {
                    var curve = AnimationUtility.GetEditorCurve(asset, binding);
                    switch (binding.propertyName)
                    {
                        case "boolMember":
                        {
                            Assert.AreEqual(2,
                                curve.keys.Select(x => x.value).Distinct().ToArray().Length);
                            break;
                        }
                        case "enumMember":
                        {
                            Assert.AreEqual(3,
                                curve.keys.Select(x => x.value).Distinct().ToArray().Length);
                            break;
                        }
                        case "intMember":
                        case "vectMember":
                        case "vectMember.x":
                        case "vectMember.y":
                        case "vectMember.z":
                        case "quatMember":
                        case "quatMember.x":
                        case "quatMember.y":
                        case "quatMember.z":
                        case "quatMember.w":
                        {
                            Assert.AreNotEqual(curve.keys.First().value, curve.keys.Last().value);
                            break;
                        }
                        case "m_Enabled":
                            break;
                        default:
                        {
                            Assert.IsTrue(false, "Unexpected animated property in MonoBehaviour");
                            break;
                        }
                    }
                }
            }
        }
    }
}
