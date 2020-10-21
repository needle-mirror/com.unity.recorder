using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.Recorder.Timeline;
using UnityEngine.TestTools;
using UnityEngine.Timeline;

namespace UnityEngine.Recorder.Tests
{
    class MovieRecorderFixture : TimelineFixture
    {
        /// <summary>
        /// The expected output file for the Timeline Recorder Clip.
        /// </summary>
        private FileInfo fileExpectedMovieFromTimeline;

        /// <summary>
        /// The expected output file for the RecorderSettings Clip.
        /// </summary>
        private FileInfo fileExpectedMovieFromController;

        /// <summary>
        /// The Recorder Settings for the scripted Recorder emulating a Recorder Window.
        /// </summary>
        private RecorderControllerSettings controllerSettings;

        /// <summary>
        /// Record a Timeline Recorder Clip and a Recorder Controller.
        /// </summary>
        /// <returns></returns>
        private void Record()
        {
            RecorderOptions.VerboseMode = false;

            // 1) Start recording the Recorder Controller
            var testRecorderController = new RecorderController(controllerSettings);
            testRecorderController.PrepareRecording();
            testRecorderController.StartRecording();

            // 2) Play the Timeline
            director.Play();
        }

        [SetUp]
        public void Setup()
        {
            // 1) Timeline Clip is 1s
            recorderClip.start = 0.0f;
            recorderClip.duration = 1.0f;

            recorderTimeline.durationMode = TimelineAsset.DurationMode.FixedLength;
            recorderTimeline.fixedDuration = 5.0f;

            // 2) Configure Timeline clip Recorder settings
            var outputPath = Application.dataPath + "/../RecordingTests/movie_test_from_timeline_";
            fileExpectedMovieFromTimeline = new FileInfo($"{outputPath}001.mp4");
            var recorderSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            recorderSettings.OutputFile = outputPath + DefaultWildcard.Take;
            recorderSettings.ImageInputSettings = new CameraInputSettings
            {
                Source = ImageSource.MainCamera,
                OutputWidth = 320,
                OutputHeight = 240,
                CameraTag = "MainCamera",
                RecordTransparency = false,
                CaptureUI = false
            };
            recorderSettings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
            recorderSettings.VideoBitRateMode = VideoBitrateMode.High;
            ((RecorderClip)recorderClip.asset).settings = recorderSettings;

            if (fileExpectedMovieFromTimeline.Exists)
                fileExpectedMovieFromTimeline.Delete();

            // 3) Add a 1s Recorder Controller (as with a Recorder Window)
            controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            var videoRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            videoRecorder.name = "My Video Recorder";
            videoRecorder.Enabled = true;
            videoRecorder.VideoBitRateMode = VideoBitrateMode.High;
            videoRecorder.ImageInputSettings = new GameViewInputSettings
            {
                OutputWidth = 320,
                OutputHeight = 240
            };
            videoRecorder.AudioInputSettings.PreserveAudio = true;
            videoRecorder.OutputFile = Application.dataPath + "/../RecordingTests/movie_test_from_controller";
            fileExpectedMovieFromController = new FileInfo(videoRecorder.OutputFile + ".mp4");
            controllerSettings.AddRecorderSettings(videoRecorder);
            controllerSettings.SetRecordModeToFrameInterval(0, 29); // 1s @ 30 FPS
            controllerSettings.FrameRate = 30;

            if (fileExpectedMovieFromController.Exists)
                fileExpectedMovieFromController.Delete();
        }

        [TearDown]
        public new void TearDown()
        {
            // Confirm there is a warning and then clean up
            fileExpectedMovieFromTimeline.Refresh();
            fileExpectedMovieFromController.Refresh();
            Assert.IsTrue(fileExpectedMovieFromTimeline.Exists, $"Expected file {fileExpectedMovieFromTimeline.FullName} doesn't exist.");
            fileExpectedMovieFromTimeline.Delete();
            Assert.IsTrue(fileExpectedMovieFromController.Exists, $"Expected file {fileExpectedMovieFromController.FullName} doesn't exist.");
            fileExpectedMovieFromController.Delete();
        }

        [UnityTest]
        public IEnumerator MultipleMovieRecorders_ShouldGenerateWarning()
        {
            Record();

            while (director.time < recorderClip.end + 0.5f)
                yield return new WaitForEndOfFrame();

            // We expect a console warning about 2+ concurrent Movie Recorders
            Regex r = new Regex("There are two or more concurrent Movie Recorders.*");
            LogAssert.Expect(LogType.Warning, r);
        }
    }
}
