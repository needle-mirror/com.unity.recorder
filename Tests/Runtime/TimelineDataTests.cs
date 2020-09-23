using NUnit.Framework;
using UnityEditor.Recorder.Timeline;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEngine.Recorder.Tests
{
    class TimelineFixture : BaseFixture
    {
        protected PlayableDirector director;
        protected GameObject cube;
        protected TimelineClip recorderClip;

        [SetUp]
        public new void SetUp()
        {
            var curve = AnimationCurve.Linear(0, 0, 10, 10);
            var clip = new AnimationClip {hideFlags = HideFlags.DontSave};
            clip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.hideFlags = HideFlags.DontSave;
            var aTrack = timeline.CreateTrack<AnimationTrack>(null, "CubeAnimation");
            aTrack.CreateClip(clip).displayName = "CubeClip";

            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.AddComponent<Animator>();
            director = cube.AddComponent<PlayableDirector>();
            director.playableAsset = timeline;
            director.SetGenericBinding(aTrack, cube);

            recorderClip = timeline.CreateTrack<RecorderTrack>(null, "RecorderTrack").CreateDefaultClip();
        }
    }
}
