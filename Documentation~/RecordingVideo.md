# Record a video in Full HD MP4

Movie Recorder supports generating videos in the H.264 MP4, VP8 WebM, or ProRes QuickTime formats.

Use the following example to configure and record a video with the most common format (MP4), resolution (1920 x 1080), and aspect ratio (16:9).  For information on using other formats, see [Movie Recorder properties](RecorderMovie.md).

## Choose a recording method

Choose the recording method that corresponds to your expected recording workflow:
* [A centralized recording session with one or multiple recorders (via the Recorder window)](get-started-recorder-window.md), OR
* [One or multiple recordings triggered from Timeline (via Recorder Clips)](get-started-timeline-track.md).

## Select the recorder type

To record a video, you have to use a **Movie Recorder**.

## Configure the Input properties

Input properties define the source of the recording and its visual parameters.

1. For **Source**, select **Game View**. If you want to use a different camera, select **Targeted Camera** and select the camera.

2. For **Output Resolution**, select **FHD - 1080**, which is Full HD 1920 x 1080.

3. For **Aspect Ratio**, select **16:9 (1.7778)**.

## Configure the Output Format properties

The Output Format properties define the media format to save the recorded frames in.

>[!NOTE]
>* The alpha channel is available for some encoders subject to the following conditions:
  * The render pipeline is High Definition Render Pipeline (HDRP) or Built-in render pipeline.
  * The source is not Game View. The other sources can support alpha.
>* To enable recording when Include Audio is selected, in **Project Settings** > **Audio** > **Default Speaker Mode**, ensure that **Mono** or **Stereo** is selected.

In H.264 MP4, you can include audio, but alpha is not available.

1. For **Encoder**, select **Unity Media Encoder**. H.264 MP4 is selected by default.

2. For **Encoding quality**, select **Low**, **Medium**, **High** or **Custom**. The lower the quality, the smaller the file size.

## Additional resources

For more complete information on Recorder properties, refer to the following:
* [General recording properties](RecorderWindowRecordingProperties.md)
* [Recording from a Timeline Track](RecordingTimelineTrack.md)
* [Movie Recorder properties](RecorderMovie.md)
* [Output File properties](OutputFileProperties.md)

You can also configure the Movie Recorder to use a [custom FFmpeg encoder](samples-custom-encoder.md) to generate video.
