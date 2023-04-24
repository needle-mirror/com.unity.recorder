# Audio Recorder properties

The **Audio Recorder** generates an audio clip in WAV format.

This page covers all properties specific to the Audio Recorder type.

> **Note:**
>* To fully configure any Recorder, you must also set the general recording properties according to the recording interface you are using: the [Recorder window](RecorderWindowRecordingProperties.md) or a [Recorder Clip](RecordingTimelineTrack.md#recorder-clip-properties).
>* Only Mono or Stereo recording is supported. To enable recording, ensure that, in **Project Settings** > **Audio** > **Default Speaker Mode**, either **Mono** or **Stereo** is selected.
>*  During recording, the audio signal is sent to the Recorder, not to your system's audio output.

![](Images/RecorderAudio.png)

The Audio Recorder properties fall into two main categories:
* [Output Format](#output-format)
* [Output File](#output-file)

## Output Format

Use this section to set up the media format you need to save the recorded images in.

|Property|Function|
|:---|:---|
| **Format** | The encoding format for Recorder output. **WAV** is the only possible option. |

## Output File

Use this section to specify the output **Path** and **File Name** pattern to save the recorded audio file.

> **Note:** [Output File properties](OutputFileProperties.md) work the same for all types of recorders.
