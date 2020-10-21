# Known issues and limitations

This page lists some known issues and limitations that you might experience with the Unity Recorder. It also gives basic instructions to help you work around them.

#### Recorder window docking & frozen recording

**Issue:** If you dock the Recorder window in the same panel as the Game view, the recording hangs forever on the first frame when you start it.

**Workaround:** Undock the Recorder window or dock it elsewhere.

#### Audio recording limited support

**Limitation:** The Recorder currently supports only the recording of samples from the Unity built-in audio engine. As such, it cannot record audio from third-party audio engines such as FMOD Studio or Wwise, for example.

**Workaround:** For a movie, you can record the third-party audio output in WAV format through another application, reimport this recorded file into the Unity Timeline, and then use the Unity Recorder to create the final movie with audio. Alternatively, you can use any video editing software to recompose audio and video.
