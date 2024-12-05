# Record with alpha

Recorder can capture the alpha channel and output transparency under certain conditions that depend on the render pipeline you're using and the Recorder settings.

## Configure the active Render Pipeline

You must set up the active render pipeline to output alpha:

| Render Pipeline | Guide to setup requirements |
| :--- | :--- |
| With High-Definition RP (HDRP) | Refer to [Alpha channel configuration](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/Alpha-Output.html) |
| With Univeral RP (URP) version 17+ | Refer to [Alpha processing setting](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/whats-new/urp-whats-new.html#alpha-processing-setting-in-post-processing) |

## Choose a recording method

The [two available recording methods](get-started.md) support alpha recording.

## Select the recorder type

You can record alpha with a **Movie Recorder** or an **Image Sequence Recorder**.

## Configure the Recorder

### Input properties

You must set the `Source` to a `Targeted Camera`, as the `Game View` doesn't support the alpha channel.

### Output Format properties

1. Select an output format that supports the alpha channel, for example:
    * With an Image Sequence Recorder, select PNG or EXR.
    * With a Movie Recorder, select MP4 or use any encoder that supports transparency.
2. Make sure to enable the `Include Alpha` option.

## Additional resources

* [Get started with the Recorder](get-started.md)
* [Movie Recorder properties](RecorderMovie.md)
* [Image Sequence Recorder properties](RecorderImage.md)
