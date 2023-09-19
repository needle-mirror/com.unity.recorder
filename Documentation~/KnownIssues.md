# Known issues and limitations

This page lists some known issues and limitations that you might experience with the Recorder. It also gives basic instructions to help you work around them.

#### Recording slowdown with concurrent Movie Recorders

**Limitation:** The Unity Editor playback process might substantially slow down if you perform concurrent recordings with multiple Movie Recorders, particularly with large image resolutions (full HD or higher).

**Workaround:** The recommended use case is to limit yourself to one Movie recording at a time. Ensure that you have only one active Movie Recorder in the Recorder window and no Movie Recorder Clips in Timeline, or vice-versa. If you need to keep concurrent recordings for some reason, you can still set up lower resolutions or try different encoders.

#### ActiveCamera recording not available with SRPs

**Limitation:** The use of a Scriptable Render Pipeline ([SRP](https://docs.unity3d.com/Manual/ScriptableRenderPipeline.html)) in your project prevents you from setting ActiveCamera as the source of the recording in the [Movie Recorder](RecorderMovie.md#targeted-camera-source-properties) and the [Image Sequence Recorder](RecorderImage.md#targeted-camera-source-properties). This render pipeline limitation applies to all SRPs including Unity's High Definition Render Pipeline ([HDRP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest)) and Universal Render Pipeline ([URP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest)).

**Workaround:** You can use a Tagged Camera for the recording. In your project, add a [Tag](https://docs.unity3d.com/Manual/Tags.html) to the camera you want to record through, and then in the Recorder Settings, select TaggedCamera and specify your camera's Tag.

#### Audio recording limited support

**Limitation:** The Recorder currently supports only the recording of samples from the Unity built-in audio engine. As such, it cannot record audio from third-party audio engines such as FMOD Studio or Wwise.

**Workaround:** For a movie, you can record the third-party audio output in WAV format through another application, reimport this recorded file into the Unity Timeline, and then use the Recorder to create the final movie with audio. Alternatively, you can use any video editing software to recompose audio and video.

**Limitation:** Only Mono or Stereo audio recording is supported. If the project uses more than two audio channels, Recorder Clips that include audio are skipped during Play mode, and, in the Recorder window, Recorders that include audio cannot be started.

**Workaround:** In **Project Settings** > **Audio** > **Default Speaker Mode**, select **Mono** or **Stereo** depending on what the encoder specified for the recording supports.

#### MP4 and ProRes encoding not supported on Linux

**Limitation:** The Movie Recorder doesn't support H.264 MP4 and ProRes QuickTime encoding on Linux.

#### Recording discontinuous animations results in continuous animation curve

**Limitation:** When you use a single recorder to record an animation sequence that includes discontinuities (for example, camera cuts), the Recorder interpolates and smoothens all discontinuities in the resulting animation curve, as it is by design in Unity. However, this process alters the expected discontinuities in the recorded animation.

**Workaround:** To keep discontinuities while recording animations, you need to perform several recordings between the cuts. For example, you could set up several Recorder clips in Timeline, relative to the source animations you need to record.

#### UNC paths not supported as output locations

**Limitation:** The Recorder output file path field doesn't support Universal Naming Convention (UNC) strings for targeting shared network folders.

**Workaround:** To target a shared network folder as the output location, specify the path to a drive you previously mapped to the network folder you're targeting.

#### Building a project with Recorder tracks generates errors

**Known issue:** When you build a project that includes Recorder tracks in Timeline, Unity throws an error in the Console. Recorder tracks are not supported in standalone builds, but Unity can't disable them at build time.

**Workaround:** Before building a project, make sure to delete or disable any Recorder tracks present in Timeline.

#### Simulator view recording not supported

**Limitation:** The Movie Recorder and Image Sequence Recorder can only record the Unity Editor output rendering view in its _Game_ state, and not in its _Simulator_ state. If the Simulator view is selected when you start recording the Game View, the source window automatically switches to Game view.

<a name="360-view"></a>
#### 360 View recording issues and limitations

The Recorder doesn't fully support 360 View recording. Here is a list of known issues you might encounter:

* If you record a 360 View in projects that use the High Definition Render Pipeline (HDRP), the rendered cube map might have artefacts that make its boundaries visible. One way to work around this issue would be to disable post-process effects such as Shadows, Bloom, or Volumetric Fogs.

* The Recorder doesn't support stereoscopic recording in projects that use any Scriptable Render Pipelines (SRPs). The **Stereo Separation** property has no effect on the recorded views, which makes the rendering identical for both eyes.

#### Overexposed frames when accumulating path tracing

**Known issue:** If a scene contains an Exposure post-process in an automatic mode, during accumulation the exposure is re-evaluated on every sub-frame. If Adaptation is set to Progressive, and path tracing is enabled, the added time to change the exposure can result in images that are incorrect (too bright/overexposed) because the first sub-frames are noisy.

**Workaround:**
1. Depending on where Exposure is set, go to the [HDRP Global Settings window](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Default-Settings-Window.html) or the Volume component override in you scene.
2. Under Volume Profiles (or Volume), expand **Exposure**.
3. Under Adaptation, set **Mode** to **Fixed**.

#### Path-tracing limitations in HDRP apply to Recorder Accumulation

Limitations to path tracing in HDRP also apply to path tracing in Accumulation. See Path tracing [limitations](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Ray-Tracing-Path-Tracing.html%23limitations).

#### Poor image quality if motion blur is applied in HDRP and Recorder Accumulation

**Limitation:** The HDRP motion-blur post-process is applied on the final image, so it is applied on top of accumulated motion blur. This creates undesirable results.

**Workaround:** Disable motion blur in HDRP before recording motion blur using Accumulation.

#### Path-tracing quality problems when capturing motion blur

**Limitation:** Motion-blur accumulation settings can negatively affect path-tracing accumulation. For example, a short Shutter Interval reduces the number of path-tracing samples accumulated.

**Workaround:** Disable [path-tracing overrides on volumes](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@@latest/index.html?subfolder=/manual/Ray-Tracing-Path-Tracing.html%23adding-path-tracing-to-a-scene) in your scene.

#### Poor image quality if anti-aliasing applied in HDRP and Recorder Accumulation

**Limitation:** Recording accumulation while anti-aliasing is enabled in HDRP may have unintended effects on image quality.

**Workaround:** Disable anti-aliasing in HDRP before recording with Accumulation. Enable **Anti-aliasing** in Accumulation instead.

#### Progress bar is visible on frames when Accumulation is enabled

**Known issue:** The Accumulation progress bar is sometimes visible at the bottom of a frame.

**Workaround:** Leave the [Game View visible](RecordingAccumulation.md#game-view-visibility) at all times for the whole duration of the recording.