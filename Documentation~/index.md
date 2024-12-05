# Recorder package

![Recorder](Images/RecorderSplash.png)

>[!NOTE]
>To use the Recorder package, you must install it separately from the Unity Editor. For detailed information about package requirements and installation instructions, refer to [Installation](installation.md).

Use the Recorder package to capture and save data from your Unity project in **[Play mode](https://docs.unity3d.com/Manual/GameView.html)**:

* Capture gameplay or a cinematic and save it as a video file, as an animated GIF, or as a sequence of separate image files.

* Capture accumulated sub-frames to get motion blur effect or path tracing convergence on the final recorded frames.

* Capture Arbitrary Output Variable (AOV) render pass data from your Scene and save it as multi-part EXR files for further image compositing.

* Capture the movements of a targeted character or camera from your Scene and record them as an animation file.

* Capture audio from Play mode to save it as a separate WAV audio file.

>[!WARNING]
>The Recorder is designed to work only in the Unity Editor in Play mode. It does not work in standalone Unity Players or builds.

| Section | Description |
| :--- | :--- |
| [Installation](installation.md) | Install the Recorder package. |
| [Recorder concepts and features](concepts-and-features.md) | Learn about the types of recording workflows you can use with the Recorder package, get a description of all recorder types available by default in this package and the ones available in conjunction with other packages, and see how you can extend the package functionality via its scripting API. |
| [Get started](get-started.md) | Set up and launch recordings according to the workflow that best suits your needs: single recording session with multiple simultaneous recorders, or multiple independent recordings triggered from Timeline. |
| [Record videos and image sequences](RecordingExamplesofUse.md) | Get instructions and recommendations to achieve different use cases provided as examples for video and image sequence recording: video in Full HD MP4, animated GIF, or image sequence with linear sRGB color space. |
| [Record with Accumulation](RecordingAccumulation.md) | Get concepts and recommendations about sub-frame accumulation recording, and record with accumulation to produce motion blur effect or path tracing convergence. |
| [Record AOVs](aov-concepts.md) | Get concepts about Arbitrary Output Variable (AOV) recording and use cases for further compositing. |
| [Launch recordings from the command line](CommandLineRecorder.md) | Set up your Unity project to launch recordings from the command line. Use this setup to enable batch recording via a job queue of many command lines targeting different recordings.  |
| [Recorder types and properties](RecorderProperties.md) | Get the description of all Recorder properties: recording session properties, input and output properties for all available types of recorders. |
| [Samples](samples.md) | Use the samples provided with the Recorder package to experiment with some specific features and use cases. For example, integrate FFmpeg as a custom command line encoder in the Movie Recorder. |
| [Known issues and troubleshooting](troubleshooting.md) | Get information about the Recorder package known issues and limitations and enable tools to help you debug recorders. |
