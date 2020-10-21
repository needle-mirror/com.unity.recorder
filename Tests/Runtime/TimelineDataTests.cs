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
        protected TimelineAsset recorderTimeline;

        [SetUp]
        public new void SetUp()
        {
            var curve = AnimationCurve.Linear(0, 0, 10, 10);
            var clip = new AnimationClip {hideFlags = HideFlags.DontSave};
            clip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            recorderTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
            recorderTimeline.hideFlags = HideFlags.DontSave;
            var aTrack = recorderTimeline.CreateTrack<AnimationTrack>(null, "CubeAnimation");
            aTrack.CreateClip(clip).displayName = "CubeClip";

            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.AddComponent<Animator>();
            director = cube.AddComponent<PlayableDirector>();
            director.playableAsset = recorderTimeline;
            director.SetGenericBinding(aTrack, cube);

            recorderClip = recorderTimeline.CreateTrack<RecorderTrack>(null, "RecorderTrack").CreateDefaultClip();
        }
    }
}
