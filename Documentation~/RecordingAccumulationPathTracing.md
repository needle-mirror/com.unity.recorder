# Accumulate path tracing

If you are using path tracing in your project, to converge on a clean image, you must use Accumulation. Otherwise, only one path-tracing sample is recorded per frame and the resulting images in the recording will be very noisy.

Recorder uses path-tracing settings defined in HDRP using the [Path Tracing Volume Override](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Ray-Tracing-Path-Tracing.html%23adding-path-tracing-to-a-scene), however the Maximum Samples property is not used.  

HDRP and Recorder accumulate samples differently:
* In HDRP, path-tracing samples accumulate over time to a specified maximum.  
* In Recorder, the number of samples you define are accumulated on each frame. This number is used throughout the recording, even if there are multiple volumes with path-tracing overrides in the recording.

You can configure Recorder to accumulate [path tracing and motion blur](RecordingAccumulationPathTracing.md#configure-path-tracing-accumulation-with-motion-blur) or to accumulate [path tracing without motion blur](RecordingAccumulationPathTracing.md#configure-path-tracing-accumulation-without-motion-blur).

>**Notes:**
* If a scene contains an “Exposure” post-process in an automatic mode and Adaptation is set to Progressive, the resulting image may be too bright when path tracing is accumulated. For more information, see [Overexposed frames when accumulating path tracing](KnownIssues.md#overexposed-frames-when-accumulating-path-tracing).<br/><br/>
* Limitations to path tracing in HDRP also apply to path tracing in Accumulation. See Path tracing [limitations](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Ray-Tracing-Path-Tracing.html%23limitations).

## Requirements
A number of specific conditions must be met to use path-tracing accumulation in your recording. See [Requirements](RecordingAccumulation.md#requirements).

## Set up path tracing
To accumulate path tracing in Recorder, ray tracing and path tracing must be configured in HDRP.

To set up path tracing for Accumulation:

1. Follow the instruction to set up ray tracing in HDRP. See [Integrating ray tracing into your HDRP Project](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Ray-Tracing-Getting-Started.html%23integrating-ray-tracing-into-your-hdrp-project).

2. Add a [path tracing override](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Ray-Tracing-Path-Tracing.html?q=Path%23adding-path-tracing-to-a-scene) to your scene.

## Configure path-tracing accumulation with motion blur

If path tracing is set up for Accumulation, accumulating motion blur also accumulates path tracing. Follow the steps in [Accumulate motion blur](RecordingAccumulationMotionBlur.md) to configure path-tracing accumulation with motion blur.

Accumulation properties in Recorder (Shutter Interval, Shutter Profile, and Samples) all apply to path-tracing accumulation. Path-tracing samples are accumulated only in the sub frames within the Shutter Interval, and the Shutter Profile controls the visibility of the path-tracing samples of each sub frame rendered on the frame.

To accumulate path tracing and motion blur, the value specified for Shutter Interval must be greater than 0.  When the Shutter Interval is greater than 0, sub frames are incremental points in time between one frame and the next.

If the positions of objects change from sub frame to sub frame in the Shutter Interval, motion blur will be visible in the converged image.

>**Tip:** If the converged images produced by Accumulation are too noisy, you can increase the number of sub frames in the Shutter Interval by increasing the value of the Samples property.

## Configure path-tracing accumulation without motion blur

You can disable motion-blur accumulation during path-tracing accumulation by setting the Shutter Interval to 0.

When the Shutter Interval is 0, time does not advance between one sub frame and the next, so the positions of objects do not change. Path-tracing samples are accumulated for every sub frame specified in the Samples property. Information from these sub frames is rendered as a converged image on the frame.

To access Accumulation properties:

* Open the [Recorder window](RecordingRecorderWindow.md), and select or add a **Movie Recorder** or an **Image Sequence Recorder**.
* Add or select a [Recorder Clip](RecordingTimelineTrack.md). In the Inspector, ensure the **Selected Recorder** is set to **Movie** or **Image Sequence**.

1. For **Source**, select **Game View** or **Targeted Camera**.
2. Expand **Input**.
3. Enable **Accumulation**.

To configure path tracing without motion blur:

1.  In **Samples** enter the total number of sub frames desired between one frame and the next. Accumulation captures a single path-tracing sample per sub frame.

2. Set the **Shutter Interval** to **0** to disable motion-blur accumulation. Shutter Profile settings are not applied.

For more information, see [Accumulation properties](RecorderAccumulationProperties.md).

## Additional resources

[Understand sub-frame capture](RecorderAccumulationUnderstandSubFrameCapture.md)

To completely configure a Movie recorder and start the recording, see [Record a video](recordingVideo.md).
