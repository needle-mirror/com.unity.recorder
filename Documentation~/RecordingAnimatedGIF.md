# Record an animated GIF

Configure an animated GIF in Movie recorder using properties in the Output Format section of the recorder.

To open or add a Movie recorder, configure General, Input and Output File properties, and start recording, see [Record a video](RecordingVideo.md).

All of the Input sources (Game View, Targeted Camera, 360 View, Render Texture Asset, and Texture Sampling) support animated GIFs. For information on using these formats, see [Movie Recorder properties](RecorderMovie.md).
>**Note:**
>The alpha channel is available subject to the following conditions:
>* The render pipeline is High Definition Render Pipeline or Built-in render pipeline.
>* The source is not Game View. The other sources can support alpha.

To configure an animated GIF recording:
1. In the Movie recorder, expand the **Output Format** section.
2. For **Encoder**, select **GIF Encoder**.
3. Specify the **Quality**. The lower the quality the smaller the file.
4. **Loop** is enabled by default. Disable Loop to stop automatic replay.
5. To include the alpha channel enable **Include alpha**.

## Additional resources

For more complete information on Recorder properties, refer to the following:
* [General recording properties](RecorderWindowRecordingProperties.md)
* [Recording from a Timeline Track](RecordingTimelineTrack.md)
* [Movie Recorder properties](RecorderMovie.md)
* [Output File properties](OutputFileProperties.md)
