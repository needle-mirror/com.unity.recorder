# Recording Accumulation

Accumulation captures information from [multiple subframes](RecorderAccumulationUnderstandSubFrameCapture.md) and combines the information to render a final "converged" frame.

Use Accumulation to:
* Produce motion blur on objects in the image that move during the exposure of the frame. The motion can be the result of objects moving in the scene or camera moves. To configure motion-blur accumulation in Recorder, see [Accumulate motion blur](RecordingAccumulationMotionBlur.md).

* Record multiple [path-tracing samples](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Ray-Tracing-Path-Tracing.html) to converge on a clean image on each frame. To configure path-tracing accumulation in Recorder, see [Accumulate path tracing](RecordingAccumulationPathTracing.md).

Accumulation automatically applies a filter to reduce artifacts in spotlight shadows. This filter requires HDRP 14.0.2 or later.

>**Notes:**
* Recording Accumulation while anti-aliasing is enabled in HDRP may have unintended effects on image quality. Disabling anti-aliasing in HDRP before recording is recommended. Enable Anti-aliasing in Accumulation instead.
* Similarly, before you record motion blur using Accumulation, disable motion blur in HDRP.

## Requirements

The use of the **Accumulation** feature is subject to very specific conditions:
* Your project must use [High Definition Render Pipeline (HDRP)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest).
* The **Game View** must be always visible during the recording.
* The Accumulation feature is available only for **Movie** or **Image Sequence** recorders.
* You can only select **Game View** or **Targeted Camera** as the **Source** for the recording.
* You can only use one active Recorder at a time when you record accumulation.

In addition to the requirements above, path-tracing accumulation requires DX12.

## Game View must be visible
When the Game View is hidden, for example, behind a Scene View, Unity can't reach [`WaitForEndOfFrame`](https://docs.unity3d.com/ScriptReference/WaitForEndOfFrame.html). If this happens during a recording, the Recorder stays in a waiting state but the simulation keeps moving forward, and so does the game clock.

As a result:
* As soon as and as long as the Game View is hidden, the Recorder can't record any frames of the simulation being played.
* If you temporarily hide the Game View during accumulation, [a progress bar becomes visible in the recorded frames](KnownIssues.md#progress-bar-is-visible-on-frames-when-accumulation-is-enabled) due to sub-frame synchronization issue between Recorder and HDRP.

## Accumulation disables concurrent recording

A recorder with Accumulation cannot be run at the same time as any other recorders. The rules are similar for Recorder window recorders and recorder clips in the Timeline.

### Recorder window

If other recorders are enabled, enabling a recorder with Accumulation disables the Start button.

You must disable (deselect) all other recorders before starting a recorder with Accumulation. Likewise, you must disable recorders with Accumulation before starting other recorders.

>**Note:** Ensure that there are no Recorder Clips in the timeline that are active at the same time as the recorder with Accumulation.

### Timeline Recorder Clips

Ensure that no other Recorder Clips are active during the Start and End times of a Recorder Clip with Accumulation. Enabling Accumulation in a Recorder Clip disables all the Recorder Clips that are active at the same time, including the Recorder Clip with Accumulation.

## Limitations

### Game View visibility

[Progress bar is visible on frames when Accumulation is enabled](KnownIssues.md#progress-bar-is-visible-on-frames-when-accumulation-is-enabled). This happens if you temporarily hide the Game View.

### Motion blur

[Path-tracing quality problems when capturing motion blur](KnownIssues.md#path-tracing-quality-problems-when-capturing-motion-blur)

[Poor image quality if motion blur applied in HDRP and Recorder Accumulation](KnownIssues.md#poor-image-quality-if-motion-blur-is-applied-in-hdrp-and-recorder-accumulation)

### Path tracing

[Overexposed frames when accumulating path tracing](KnownIssues.md#overexposed-frames-when-accumulating-path-tracing)

[Path-tracing limitations in HDRP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Ray-Tracing-Path-Tracing.html%23limitations). Limitations also apply to path tracing in Accumulation.

### Anti-aliasing

[Poor image quality if anti-aliasing applied in HDRP and Recorder Accumulation](KnownIssues.md#poor-image-quality-if-anti-aliasing-applied-in-hdrp-and-recorder-accumulation)

## Additional resources

Accumulation in HDRP, see [Multiframe rendering and accumulation](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Accumulation.html).
