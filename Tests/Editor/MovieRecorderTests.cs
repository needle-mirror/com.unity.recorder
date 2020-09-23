using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.Recorder;
using UnityEngine.TestTools;

namespace UnityEngine.Recorder.Tests
{
    class MovieRecorderTests
    {
        [UnityTest]
        public IEnumerator RecordingWithInvalidSettingsShouldNotPreventSubsequentRecords()
        {
            yield return new EnterPlayMode();
            CreateRecorderInstances(out var controller, out var movieSettings);
            movieSettings.ImageInputSettings.OutputHeight = 101;
            movieSettings.ImageInputSettings.OutputWidth = 101;
            controller.PrepareRecording();
            controller.StartRecording();
            controller.StopRecording();
            LogAssert.Expect(LogType.Error, "The MP4 format does not support odd values in resolution");
            Object.DestroyImmediate(controller.Settings);
            Object.DestroyImmediate(movieSettings);

            yield return new ExitPlayMode();
            yield return new EnterPlayMode();
            CreateRecorderInstances(out controller, out movieSettings);
            movieSettings.OutputFile = "Assets/tmp";
            movieSettings.ImageInputSettings.OutputHeight = 100;
            movieSettings.ImageInputSettings.OutputWidth = 100;
            controller.PrepareRecording();
            controller.StartRecording();
            yield return null;
            controller.StopRecording();
            Object.DestroyImmediate(controller.Settings);
            Object.DestroyImmediate(movieSettings);
            var outputFileName = movieSettings.OutputFile + ".mp4";
            Assert.True(File.Exists(outputFileName));
            File.Delete(outputFileName);
        }

        void CreateRecorderInstances(out RecorderController controller, out MovieRecorderSettings movieSettings)
        {
            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();

            settings.AddRecorderSettings(movieSettings);
            controller = new RecorderController(settings);
        }
    }
}
