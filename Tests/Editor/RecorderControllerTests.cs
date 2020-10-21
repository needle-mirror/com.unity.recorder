using System;
using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.Recorder.Tests
{
    class RecorderControllerTests
    {
        [Test]
        public void PrepareRecording_InNonPlayMode_ShouldThrowsException()
        {
            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            var recorderController = new RecorderController(settings);

            var ex = Assert.Throws<Exception>(() => recorderController.PrepareRecording());
            Assert.IsTrue(ex.Message.Contains("You can only call the PrepareRecording method in Play mode."));
            UnityObject.DestroyImmediate(settings);
        }

        [UnityTest]
        public IEnumerator StartAndStopRecording_IncreasesTakeByOne()
        {
            yield return new EnterPlayMode();
            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            settings.SetRecordModeToTimeInterval(0, 20);
            var movSettings = ScriptableObject.CreateInstance<ImageRecorderSettings>();
            settings.AddRecorderSettings(movSettings);
            var recorderController = new RecorderController(settings);

            recorderController.PrepareRecording();
            Assert.AreEqual(1, movSettings.Take);
            recorderController.StartRecording();
            yield return null;
            recorderController.StopRecording();
            yield return new ExitPlayMode();
            Assert.AreEqual(2, movSettings.Take);
            UnityObject.DestroyImmediate(settings);
            UnityObject.DestroyImmediate(movSettings);
        }

        [UnityTest]
        public IEnumerator StartAndStopRecordingGif_When_Pause_DoesNotCrash()
        {
            yield return new EnterPlayMode();
            EditorApplication.isPaused = true;
            yield return null;
            Assert.AreEqual(EditorApplication.isPaused, true);

            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            settings.SetRecordModeToTimeInterval(0, 20);
            var gifSettings = ScriptableObject.CreateInstance<GIFRecorderSettings>();
            settings.AddRecorderSettings(gifSettings);
            var recorderController = new RecorderController(settings);

            recorderController.PrepareRecording();
            recorderController.StartRecording();
            yield return null;
            recorderController.StopRecording();
            yield return new ExitPlayMode();
            UnityObject.DestroyImmediate(settings);
            UnityObject.DestroyImmediate(gifSettings);
        }
    }
}
