# Understand sub-frame capture

When Accumulation is enabled, the image that is rendered on each frame is based on the sub frames accumulated during the recording. The Samples and Shutter Interval properties define the sub frames to accumulate. The Shutter Profile property defines the weight applied to each sub frame in the rendered image.

>**Notes:**
>* Sub frames that precede the first frame, as defined by the Start Frame property, are not captured on the first frame. Accumulation begins in the interval before the second frame.<br/>

>* The number of samples (sub frames) in the diagram below is for illustration only. Hundreds of samples may be needed to produce a smooth motion-blur trail; thousands of path-tracing samples may be needed to converge on a clean image.

![Image shows that the interval between one frame and the next is divided into sub frames, shutter interval starts immediately after the last frame and contains a number of the sub frames; the shutter profile is fully contained in the interval, and the sub frames in the interval are recorded on the following frame.](Images/recorder-accumulate-properties.png)

| Item | Description |
| :--- | :--- |
| **A** | **Frame**. Accumulated sub frames (C) are recorded on the next frame (A). |
| **B** | **Samples**. Defines the number of sub frames between one frame and the next. |
| **C** | **Shutter Interval**.<ul><li>If greater than 0, each sub frame in the Shutter interval is accumulated. Only these sub frames are recorded on the frame.</li><li>If equal to 0, motion blur is turned off. Path-tracing is accumulated for each sub frame defined in Samples (B).</li></ul>See additional information [below](#shutterInterval-explained). |
| **D** | **Shutter Profile**. Defines the weight that each sub frame in the Shutter Interval contributes to the rendered image recorded on the frame. |

<href id="shutterInterval-explained"/> The Shutter Interval affects sub-frame accumulation as follows:

* When the Shutter Interval is greater than 0, sub frames are incremental points in time between one frame and the next. Only sub frames within the Shutter Interval (**C**) are accumulated.

* Setting the Shutter Interval to 0 turns off motion blur accumulation. When the Shutter Interval is 0, time does not advance between one sub frame and the next. Path-tracing samples are accumulated for the number of sub frames specified in Samples (**B**).

## Additional resources

[Accumulation properties](RecorderAccumulationProperties.md)

To configure accumulation in Recorder, see:
* [Accumulate motion blur](RecordingAccumulationMotionBlur.md)
* [Accumulate path tracing](RecordingAccumulationPathTracing.md)
