# Accumulate motion blur

You can configure Accumulation to simulate the blur effect that occurs in a real-world camera when the image, or objects in the image, move during the exposure of a frame. The amount of motion blur you accumulate depends on the distance objects move from one frame to the next and the length of the interval between frames that you capture. You can also configure the weight each sub frame contributes to the rendered image recorded on the frame.

To accumulate motion blur, the value specified for Shutter Interval must be greater than 0.
When the Shutter Interval is greater than 0, sub frames are incremental points in time between one frame and the next.

>**Notes:**
* Before you record motion-blur accumulation, disable **Motion Blur** post-processes in [HDRP Global Settings](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Default-Settings-Window.html) (**Edit > Project Settings > Graphics**, and then, in the sidebar, click **HDRP Global Settings**).<br/><br/>  
* If path tracing is configured in your scene: The Accumulation properties (Shutter Interval, Shutter Profile, and Samples) also apply to path-tracing accumulation. Path-tracing samples are accumulated only for the sub frames within the Shutter Interval, and the Shutter Profile is applied. If the converged images are too noisy, you can increase the number of sub frames in the Shutter Interval by increasing the value of the Samples property. <br/>Alternatively, You may want to disable path tracing overrides on volumes in your scene. See [Adding path tracing to a scene](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Ray-Tracing-Path-Tracing.html%23adding-path-tracing-to-a-scene)

## Requirements
A number of specific conditions must be met to use motion-blur and path-tracing accumulation in your recording. See [Requirements](RecordingAccumulation.md#requirements).

## Configure motion-blur accumulation

To access Accumulation properties:  

* Open the [Recorder window](RecordingRecorderWindow.md), and select or add a **Movie Recorder** or an **Image Sequence Recorder**.
* Add or select a [Recorder Clip](RecordingTimelineTrack.md). In the Inspector,  ensure the **Selected Recorder** is set to **Movie** or **Image Sequence**.

1. For **Source**, select **Game View** or **Targeted Camera**.
2. Expand **Input**
3. Enable **Accumulation**.

To configure Accumulation to produce motion blur:

1. In **Samples** enter the total number of sub frames desired between one frame and the next.

2. Set the **Shutter Interval** to indicate the interval between the frames that will be used for Accumulation.

3. Set the **Shutter Profile** to indicate the weight to apply to each sub-frame (sample) in the Shutter Interval. <br/>
The weight is analogous to the degree that the shutter is open. In the final rendered image in which all of the sub frames are merged, a sample with a weight of 0.5 is more visible than a sample with a weight of 0.25.

For more information, see [Accumulation properties](RecorderAccumulationProperties.md).

## Additional resources

[Understand sub-frame capture](RecorderAccumulationUnderstandSubFrameCapture.md)

To completely configure a Movie recorder and start the recording, see [Record a video](recordingVideo.md).
