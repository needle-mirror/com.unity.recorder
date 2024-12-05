# Recorder concepts and features

Before you start using the Recorder package, you must be aware of two complementary concepts:
* The [recording methods](#recording-methods) available for setting up your project according to your workflow needs.
* The [recorder types](#recorder-types-available-in-this-package) available for capturing and saving data according to your output needs.

>[!WARNING]
>The Recorder is designed to work only in the Unity Editor in Play mode. It does not work in standalone Unity Players or builds.

## Recording methods

There are two conceptually different methods to set up recordings with the Recorder package.

### Centralized recording session with multiple recorders

The Recorder package allows you to [set up a recording session with one or multiple recorders for simultaneous recordings](get-started-recorder-window.md) within an arbitrary time or frame interval in Play mode.

The entry point for this type of scenario is the [Recorder window](RecordingRecorderWindow.md), where you set up a single [recording session](RecorderWindowRecordingProperties.md) and a [recorder list](RecorderManage.md) that can include one or several recorders.

The Recorder list can include recorders of different types or recorders of the same type but with different settings. When using the Recorder window, all recorders of the list share the same recording session properties, which makes all recordings start and stop synchronously and share the same recording frame rate.

### Multiple recordings triggered from Timeline

The Recorder package also allows you to [trigger independent recordings from Timeline](get-started-timeline-track.md) at specific time or frame intervals in Play mode.

The entry point for this type of scenario is the [Timeline](https://docs.unity3d.com/Packages/com.unity.timeline@latest), where you set up [Recorder Tracks with Recorder Clips](RecordingTimelineTrack.md).

You can use multiple Recorder Clips in the same Recorder Track to record data at different times of your Timeline. To record multiple types of data at the same time, you must use multiple Recorder Tracks.

>[!NOTE]
>* Only Mono or Stereo audio recording is supported. If a non-supported speaker mode is selected in **Project Settings** > **Audio** > **Default Speaker Mode**, Recorder Clips that include audio are skipped in Play mode.  
>* In Play mode, while the play head is over a Recorder Clip that records audio, the audio signal is sent to the Recorder, not to your system's audio output.

## Recorder types available in this package

The Recorder package includes the following recorder types by default:

| Recorder type | Description |
| :--- | :--- |
| [**Animation Clip Recorder**](RecorderAnimation.md) | Generates an animation clip in Unity Animation format (.anim extension). |
| [**Movie Recorder**](RecorderMovie.md) | [Generates a video](RecordingVideo.md) in H.264 MP4, VP8 WebM, or ProRes QuickTime format. Also allows to [generate animated GIF files](RecordingAnimatedGIF.md). |
| [**Image Sequence Recorder**](RecorderImage.md) | Generates a sequence of image files in JPEG, PNG, or EXR (OpenEXR) format. |
| [**Audio Recorder**](RecorderAudio.md) | Generates an audio clip in WAV format. |
| [**AOV Image Sequence Recorder**](aov-recorder-properties.md) | Generates a sequence of image files in PNG or EXR (OpenEXR) format to capture [Arbitrary Output Variable (AOV) render pass data](aov-concepts.md) for further compositing (for example, data related to materials, geometry, depth, motion, or lighting) in projects that use Unity's [HDRP (High Definition Render Pipeline)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest). |

## Additional recorder types available with other packages

You can benefit from additional Recorder types if you install specific packages along with the Recorder:

* The [Alembic for Unity](https://docs.unity3d.com/Packages/com.unity.formats.alembic@latest) package includes an **Alembic Clip Recorder**, which allows you to record and export GameObjects to an Alembic file.

* The [FBX Exporter](https://docs.unity3d.com/Packages/com.unity.formats.fbx@latest) package includes an **FBX Recorder**, which allows you to record and export animations directly to FBX files.

## Extend the Recorder package functionality

You can use the Recorder Scripting API to extend the Recorder package functionality beyond the features available by default in the Editor UI:

* This documentation includes instructions to set up your Unity project to [launch recordings from the command line](CommandLineRecorder.md) and enable batch recording.

* The Recorder package includes [various samples](samples.md) to let you experiment with some specific features and use cases.
