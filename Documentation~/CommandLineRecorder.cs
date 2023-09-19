using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.Recorder;
using UnityEngine;

public class CommandLineRecorder : MonoBehaviour
{
    // The RecorderController starts and stops the recording.
    private RecorderController m_Controller;

    // The first frame to record.
    [SerializeField] private int m_startFrame;

    // The last frame to record.
    [SerializeField] private int m_endFrame;

    // The path to the Recorder Settings preset file to use for the recording.
    [SerializeField] private string m_presetPath;

    static RecorderSettings LoadRecorderSettingsFromPreset(string presetPath)
    {
        // Load the Preset from the provided path.
        var preset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);

        // Use reflection to determine the type of the RecorderSettings to use
        // (for example a MovieRecorderSettings).
        var recorderSettingsTypes =
            TypeCache.GetTypesDerivedFrom<RecorderSettings>().ToList();

        var recorderSettingsType = recorderSettingsTypes.SingleOrDefault(
            t => t.Name == preset.GetTargetTypeName());

        if (recorderSettingsType == null)
        {
            Debug.Log("Preset must be a subclass of RecorderSettings");
            return null;
        }

        // Create a new RecorderSettings instance and apply the Preset to it.
        RecorderSettings outSettings =
            (RecorderSettings)ScriptableObject.CreateInstance(recorderSettingsType);

        preset.ApplyTo(outSettings);
        outSettings.name = preset.name;

        return outSettings;
    }

    void StartRecording(string presetPath, int startFrame, int endFrame)
    {
        // Create RecorderSettings from the provided Preset path.
        RecorderSettings recorderSettings = LoadRecorderSettingsFromPreset(presetPath);
        recorderSettings.FrameRate = 60;

        // Create a new RecorderControllerSettings to set the start and end frame for
        // the recording session and add the RecorderSettings to it.
        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        controllerSettings.AddRecorderSettings(recorderSettings);
        controllerSettings.SetRecordModeToFrameInterval(startFrame, endFrame);

        // Create and setup a new RecorderController and start the recording.
        m_Controller = new RecorderController(controllerSettings);
        m_Controller.PrepareRecording();
        m_Controller.StartRecording();
    }

    void OnEnable()
    {
        // This is called once when Unity enters PlayMode.
        StartRecording(m_presetPath, m_startFrame, m_endFrame);
    }

    void Update()
    {
        // This is called on every frame when Unity is in PlayMode.
        if (m_Controller != null && !m_Controller.IsRecording())
        {
            // When the RecorderController has no more frame to record, stop
            // the recording and exit the PlayMode.
            m_Controller.StopRecording();
            EditorApplication.ExitPlaymode();
        }
    }

    static Dictionary<string, string> GetCommandLineArgs()
    {
        string[] cmdLineParts = Environment.GetCommandLineArgs();
        Dictionary<String, String> arguments = new Dictionary<string, string>();

        // These are the 3 arguments this MonoBehaviour expects to start a recording.
        string[] args = { "-startFrame", "-endFrame", "-presetPath" };

        var idx = 1;
        while (idx < cmdLineParts.Length)
        {
            var part = cmdLineParts[idx];
            if (args.Contains(part))
            {
                var argName = part.TrimStart('-');
                arguments[argName] = cmdLineParts[idx + 1];
            }

            idx++;
        }

        return arguments;
    }

    private void SetRecordingInfo(int startFrame, int endFrame, string presetPath)
    {
        m_startFrame = startFrame;
        m_endFrame = endFrame;
        m_presetPath = presetPath;
        AssetDatabase.SaveAssets();
    }

    public static void ExecuteCommandLine()
    {
        // Parse the command line arguments to find the start and end frame and
        // the path to the Recorder Settings Preset file to use.
        var args = GetCommandLineArgs();
        if (!args.TryGetValue("startFrame", out var startFrame))
            throw new ArgumentException("[ERROR] Expected argument -startFrame");

        if (!args.TryGetValue("endFrame", out var endFrame))
            throw new ArgumentException("[ERROR] Expected argument -endFrame");

        if (!args.TryGetValue("presetPath", out var presetPath))
            throw new ArgumentException("[ERROR] Expected argument -presetPath");

        // Find the GameObject that has the CommandLineRecorder MonoBehaviour attached
        // to it and set the recording information provided by the command line arguments.
        var sceneCommandLineRecorder =
            FindObjectsByType<CommandLineRecorder>(FindObjectsSortMode.None).First();

        sceneCommandLineRecorder.SetRecordingInfo(
            Convert.ToInt32(startFrame),
            Convert.ToInt32(endFrame),
            presetPath);

        // Enter PlayMode: which starts the Recording (OnEnable)
        EditorApplication.EnterPlaymode();
    }
}
