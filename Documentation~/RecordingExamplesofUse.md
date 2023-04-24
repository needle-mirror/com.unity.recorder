# Make a recording

The procedures listed below are provided as typical uses of the Recorder feature. The same general steps are used to configure and record other Recorder outputs.
>**Note**: Recorders can be configured in the Recorder window or in the Timeline:
* Recorders configured in the [Recorder window](RecordingRecorderWindow.md) are manually started. They can be configured to record what is currently playing, a set of frames specified with a Start frame and End frame, or a time interval.
* [Recorder Clips](RecordingTimelineTrack.md), which are used to specify recorders in the Timeline, automatically start recording when the Timeline plays the specified frames.

| Procedure | Description |
| --- | --- |
| [Record a video](RecordingVideo.md) | Configure and record video in H.264 MP4, VP8 WebM, or ProRes QuickTime formats.<br/>Subject to some very specific [requirements](RecordingAccumulation.md#requirements), you can include motion-blur accumulation and path-tracing accumulation in the recording. Path tracing can also be recorded without motion blur. |
| [Record an animated GIF](RecordingAnimatedGIF.md) | Configure and record an animated GIF. |
| [Accumulate motion blur](RecordingAccumulationMotionBlur.md) | Configure motion-blur accumulation in a Movie recorder or Image Sequence recorder. |
| [Accumulate path tracing](RecordingAccumulationPathTracing.md) | Configure path-tracing accumulation in a Movie recorder or Image Sequence recorder. You can configure path-tracing accumulation with or without motion blur.|
| [Configure a recorder to use the linear sRGB (unclamped) color space](RecordingInLinearSrgbColorSpace.md) | Configure an AOV or Image Sequence recorder to use the linear sRGB (unclamped) color space. <br/>**Note:** The AOV Image Sequence recorder included in this version of the Recorder package is marked for deprecation and will be removed in Recorder 5.0.0. |
