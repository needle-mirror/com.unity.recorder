# Configure a recorder to use the linear sRGB (unclamped) color space

If your project uses [High Definition Render Pipeline (HDRP)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest), you can record images using the linear sRGB (unclamped) color profile. This setting is available in Image Sequence recorder or AOV Image Sequence recorder.

>**Note:** The AOV Image Sequence recorder included in this version of the Recorder package is marked for deprecation and will be removed in Recorder 5.0.0.

In a compositing application, some mathematical operations such as color blending are more physically accurate using linear sRGB than the sRGB curve (gamma).

In an unclamped recording, values outside of the 0.0 to 1.0 range of the linear sRGB gamut, that is, values that usually cannot be displayed on a monitor or screen, are saved in the file. These values, especially values greater than 1.0, can be useful for compositing.

## Prerequisites

Your project must use HDRP.

To get unclamped values in the output:

  * Disable Tonemapping. The Tonemapping post-process in HDRP compresses all values into the displayable range. As a result, values greater than 1.0 are not saved in the file.<br/>
  To disable Tonemapping:
    1. In the top menu of the **Editor**, select **Edit > Project Settings > HDRP Global Settings**.
    2. In **HDRP Global Settings**, disable **Tonemapping**.
    3. In the **Hierarchy**, for each volume that includes a Tonemapping override, select the volume.  In the **Inspector**, disable **Tonemapping**.

  * In the camera used for the recording, disable dithering:<br/> In the **Hierarchy**, select the camera. In the **Inspector**, under **Rendering**, disable **Dithering**.

## Configure the recorder

To access Recorder properties:

* Open the [Recorder window](RecordingRecorderWindow.md), and select or add an **AOV Recorder** or an **Image Sequence Recorder**.
* Add or select a [Recorder Clip](RecordingTimelineTrack.md). In the Inspector,  ensure the **Selected Recorder** is set to **AOV** or **Image Sequence**.

To configure the recording of linear sRGB (unclamped) values in an Image Sequence recorder or AOV recorder:

1. Under **Input**, select the source.
  * In the Image Sequence Recorder, Linear sRGB (unclamped) is available for all sources except Game View.
  * In the AOV recorder, Linear sRGB unclamped is available for all cameras.
2. Select the **Camera**.
3. For **Media File Format**, select **EXR**.
4. For **Color Space**, select **Linear, sRGB (unclamped)**.

## Additional resources

* [Color space](https://docs.unity3d.com/2023.1/Documentation/Manual/LinearLighting.html)
* [Tonemapping](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Post-Processing-Tonemapping.html)
* [AOV Image Sequence Recorder properties](RecorderAOV.md)
* [Image Sequence Recorder properties](RecorderImage.md)
