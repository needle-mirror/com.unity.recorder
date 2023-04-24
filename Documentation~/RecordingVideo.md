# Record a video

Movie Recorder generates videos in the H.264 MP4, VP8 WebM, or ProRes QuickTime formats.

Use the following example to configure and record a video with the most common format (MP4), resolution (1920 x 1080), and aspect ratio (16:9).  For information on using other formats, see [Movie Recorder properties](RecorderMovie.md).

## Open the Movie recorder

Add or select a Movie recorder in the Recorder window or in the Timeline:
  * Open the [Recorder window](RecordingRecorderWindow.md). Under **Add Recorder**, select or add a **Movie** recorder.
  * In the Timeline, add or select a [Recorder Clip](RecordingTimelineTrack.md). In the Inspector, under **Recorder Clip**, ensure the **Selected recorder** is **Movie**.

## Configure the recorder's General properties

If you are using the Recorder window, you can:
1. Enable **Exit Play mode** to exit play mode when the recording ends.

2. Choose how to start the recording:
  - **Manual** to record at any time during Play mode.
  - **Frame Interval** to specify a Start frame and End frame.
  - **Time Interval** to specify a Start and End time in seconds.<br/><br/>

3. Select a **Target FPS**.

If you are using a Recorder Clip: In the Inspector, in **Clip Timing**, you can adjust the **Start** and **End** of the clip by entering values in the **s** (second) or **f** (frame) fields. Alternatively, you can move the left or right edge of the clip in the Timeline.

## Configure the Input properties

Input properties define the source of the recording and its visual parameters.

1. For **Source**, select **Game View**. If you want to use a different camera, select **Targeted Camera** and select the camera.

2. For **Output Resolution**, select **FHD - 1080**, which is Full HD 1920 x 1080.

3. For **Aspect Ratio**, select **16:9 (1.7778)**.

## Configure the Output Format properties

The Output Format properties define the media format to save the recorded frames in.
>**Notes:**
>* The alpha channel is available for some encoders subject to the following conditions:
  * The render pipeline is High Definition Render Pipeline (HDRP) or Built-in render pipeline.
  * The source is not Game View. The other sources can support alpha.
>* To enable recording when Include Audio is selected, in **Project Settings** > **Audio** > **Default Speaker Mode**, ensure that **Mono** or **Stereo** is selected.

In H.264 MP4, you can include audio, but alpha is not available.

1. For **Encoder**, select **Unity Media Encoder**. H.264 MP4 is selected by default.

2. For **Encoding quality**, select **Low**, **Medium**, **High** or **Custom**. The lower the quality, the smaller the file size.

## Configure the Output File properties

Use these properties to specify the name of your recording and where to save it. You can use placeholders to auto generate meaningful filenames.
1. For **File name**, type the name and/or use placeholders to include auto-generated text in the filename. You can select placeholder from the **+Wildcards** list or insert them manually.  
  >**Example:** Adding the `<Take>` wildcard automatically adds the take number to the filename. The take is incremented by 1 after each recording, and the current take appears in the **Take** field.

2. For **Path**, use a preset file path from the list to specify the location to save your file. You can also manually enter or browse to the desired location to insert it into the path.

## Record the video

* If you configured the recorder in the Recorder window, see [Starting a Recording](RecorderWindowRecordingProperties.md#starting-a-recording).

* If you configured a Recorder Clip, see [Starting and stopping the recording](RecordingTimelineTrack.md#starting-and-stopping-the-recording).

>**Note:** During recording, if audio is included, the audio signal is sent to the Recorder, not to your system's audio output.

## Additional resources

For more complete information on Recorder properties, refer to the following:
* [General recording properties](RecorderWindowRecordingProperties.md)
* [Recording from a Timeline Track](RecordingTimelineTrack.md)
* [Movie Recorder properties](RecorderMovie.md)
* [Output File properties](OutputFileProperties.md)

You can also configure the Movie Recorder to use a [custom FFmpeg encoder](samples-custom-encoder.md) to generate video.
