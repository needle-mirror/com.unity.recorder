using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Recorder
{
    /// <summary>
    /// Sets which source camera to use for recording (by some specific Recorders).
    /// </summary>
    [Flags]
    public enum ImageSource
    {
        /// <summary>
        /// Use the current active camera.
        /// </summary>
        ActiveCamera = 1,
        /// <summary>
        /// Use the main camera.
        /// </summary>
        MainCamera = 2,
        /// <summary>
        /// Use the first camera that matches a GameObject tag.
        /// </summary>
        TaggedCamera = 4
    }

    /// <summary>
    /// Sets which frame rate type to use during recording.
    /// </summary>
    public enum FrameRatePlayback
    {
        /// <summary>
        /// The frame rate doesn't vary during recording, even if the actual frame rate is lower or higher.
        /// </summary>
        Constant,

        /// <summary>
        /// Use the application's frame rate, which might vary during recording. This option is not supported by all Recorders.
        /// </summary>
        Variable,
    }

    /// <summary>
    /// The mode that defines the way to manage the starting point and duration of the recording.
    /// </summary>
    public enum RecordMode
    {
        /// <summary>
        /// Record every frame between when the recording is started and when it is stopped (either using the UI or through API methods).
        /// </summary>
        Manual,

        /// <summary>
        /// Record one single frame according to the specified frame number.
        /// </summary>
        SingleFrame,

        /// <summary>
        /// Record all frames within an interval of frames according to the specified Start and End frame numbers.
        /// </summary>
        FrameInterval,

        /// <summary>
        /// Record all frames within a time interval according to the specified Start time and End time.
        /// </summary>
        TimeInterval
    }

    /// <summary>
    /// Main base class for a Recorder settings.
    /// Each Recorder needs to have its corresponding settings properly configured.
    /// </summary>
    public abstract class RecorderSettings : ScriptableObject, ISerializationCallbackReceiver
    {
        private static string s_OutputFileErrorMessage = "Recorder output file cannot be empty";

        //  Use the Windows value of 259 (260-1 for the NUL terminator)
        // (https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation)
        private int m_MaxPathLength = 259;

        /// <summary>
        /// Stores the path this Recorder will use to generate the output file.
        /// It can be either an absolute or a relative path.
        /// The file extension is automatically added.
        /// Wildcards such as <c>DefaultWildcard.Time</c> are supported.
        /// <seealso cref="DefaultWildcard"/>
        /// </summary>
        public string OutputFile
        {
            get { return fileNameGenerator.ToPath(); }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException(s_OutputFileErrorMessage);

                fileNameGenerator.FromPath(value);
            }
        }

        /// <summary>
        /// Indicates if this Recorder is active when starting the recording. If false, the Recorder is ignored and generates no output.
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        [SerializeField] private bool enabled = true;

        /// <summary>
        /// Stores the current Take number. Automatically incremented after each recording session.
        /// </summary>
        public int Take
        {
            get { return take; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException($"The take number must be positive");
                take = value;
            }
        }

        [SerializeField] internal int take = 1;

        /// <summary>
        /// Stores the file extension used by this Recorder (without the dot).
        /// </summary>
        protected internal abstract string Extension { get; }

        [SerializeField] internal int captureEveryNthFrame = 1;

        [SerializeField] internal FileNameGenerator fileNameGenerator;

        internal bool IsOutputNameDuplicate
        {
            get;
            set;
        }

        /// <summary>
        /// The object that resolves wildcards into a final path for output files.
        /// </summary>
        public FileNameGenerator FileNameGenerator => fileNameGenerator;

        /// <summary>
        /// The mode that defines the way to manage the starting point and duration of the recording: either manually
        /// or within a specific time or frame interval.
        /// </summary>
        public RecordMode RecordMode { get; set; }

        /// <summary>
        /// The type of frame rate to use in the recording: constant or variable.
        /// </summary>
        public FrameRatePlayback FrameRatePlayback { get; set; }

        float frameRate = 30.0f;

        /// <summary>
        /// The number of recorded frames per second. In constant frame rate mode, this represent a target value, while
        /// in variable frame rate mode, this represents a maximum value.
        /// </summary>
        /// <seealso cref="FrameRatePlayback"/>
        public float FrameRate
        {
            get => frameRate;
            set => frameRate = value;
        }

        /// <summary>
        /// The start frame of the recording.
        /// </summary>
        public int StartFrame { get; set; }

        /// <summary>
        /// The end frame of the recording.
        /// </summary>
        public int EndFrame { get; set; }

        /// <summary>
        /// The start time of the recording.
        /// </summary>
        public float StartTime { get; set; }

        /// <summary>
        /// The end time of the recording.
        /// </summary>
        public float EndTime { get; set; }

        /// <summary>
        /// Specifies whether or not to limit the frame rate when it is above the target frame rate.
        /// </summary>
        public bool CapFrameRate { get; set; }

        /// <summary>
        /// The constructor of the class.
        /// </summary>
        protected RecorderSettings()
        {
            fileNameGenerator = new FileNameGenerator()
            {
                Root = OutputPath.Root.Project,
                Leaf = "Recordings"
            };
            fileNameGenerator.RecorderSettings = this;
        }

        /// <summary>
        /// Tests if the Recorder is correctly configured.
        /// </summary>
        /// <param name="errors">List of errors encountered.</param>
        /// <returns>True if there are no errors, False otherwise.</returns>
        [Obsolete("Please use methods GetErrors() and GetWarnings()")]
        protected internal virtual bool ValidityCheck(List<string> errors)
        {
            var ok = true;

            if (InputsSettings != null)
            {
                var inputErrors = new List<string>();
#pragma warning disable 618
                var valid = InputsSettings.All(x => x.ValidityCheck(inputErrors));
#pragma warning restore 618
                if (!valid)
                {
                    errors.AddRange(inputErrors);
                    ok = false;
                }
            }

            return ok;
        }

        /// <summary>
        /// Tests if the Recorder has any errors.
        /// </summary>
        /// <param name="errors">List of errors encountered.</param>
        protected internal virtual void GetErrors(List<string> errors)
        {
            if (InputsSettings != null)
            {
                foreach (var i in InputsSettings)
                {
                    var inputErrors = new List<string>();
                    i.CheckForErrors(inputErrors);
                    errors.AddRange(inputErrors);
                }
            }

            if (string.IsNullOrEmpty(fileNameGenerator.FileName))
            {
                errors.Add("Missing file name");
            }

            if (IsOutputNameDuplicate)
            {
                errors.Add("Output file name is not unique");
            }

            if (Math.Abs(FrameRate) <= float.Epsilon)
            {
                errors.Add("Invalid frame rate");
            }

            if (!IsPlatformSupported)
            {
                errors.Add("Current platform is not supported");
            }

            if (this is IResolutionUser { IsOutputResolutionContradictory : true })
            {
                errors.Add("Conflicting resolution detected. All active Recorders must have the same output resolution.");
            }

            if (fileNameGenerator.BuildAbsolutePath(null).Length > m_MaxPathLength)
            {
                errors.Add($"File path length exceeds {m_MaxPathLength} characters.");
            }
        }

        /// <summary>
        /// Tests if the Recorder has any warnings.
        /// </summary>
        /// <param name="warnings">List of warnings encountered.</param>
        protected internal virtual void GetWarnings(List<string> warnings)
        {
            if (InputsSettings != null)
            {
                foreach (var i in InputsSettings)
                {
                    var inputWarnings = new List<string>();
                    i.CheckForWarnings(inputWarnings);
                    warnings.AddRange(inputWarnings);
                }
            }
        }

        /// <summary>
        /// Indicates if the current platform is supported (True) or not (False).
        /// </summary>
        public virtual bool IsPlatformSupported
        {
            get { return true; }
        }

        /// <summary>
        /// Indicates whether the input must be flipped vertically (True) or not (False) for the output format.
        /// </summary>
        /// <remarks>
        /// Some output formats might expect the image to be vertically flipped while others expect it as it comes from the engine,
        /// due to different coordinate conventions.
        /// </remarks>
        /// <seealso cref="UnityEditor.Recorder.Encoder.EncoderCoordinateConvention"/>
        internal virtual bool NeedToFlipVerticallyForOutputFormat => false;

        /// <summary>
        /// Stores the list of Input settings required by this Recorder.
        /// </summary>
        public abstract IEnumerable<RecorderInputSettings> InputsSettings { get; }

        /// <summary>
        /// Override this method if any post treatment needs to be done after this Recorder is duplicated in the Recorder Window.
        /// </summary>
        public virtual void OnAfterDuplicate()
        {
        }

        internal virtual bool IsInvalid()
        {
            return false;
        }

        protected internal virtual bool HasErrors()
        {
            var errors = new List<string>();
            GetErrors(errors);
            return errors.Count > 0;
        }

        internal virtual bool HasWarnings()
        {
            var warnings = new List<string>();
            var oldErrors = new List<string>();
#pragma warning disable 618
            // In the old API, errors were meant to be handled as non-blocking warnings
            ValidityCheck(oldErrors);
#pragma warning restore 618
            GetWarnings(warnings);
            return oldErrors.Count > 0 || warnings.Count > 0;
        }

        /// <summary>
        /// Validation of serialized value.
        /// </summary>
        internal virtual void OnValidate()
        {
            captureEveryNthFrame = Mathf.Max(1, captureEveryNthFrame);
            take = Mathf.Max(0, take);
            OnValidateUpgrade();
        }

        /// <summary>
        /// Indicates whether the current Recorder supports Accumulation recording or not.
        /// </summary>
        /// <returns>True if the current Recorder supports Accumulation recording, False otherwise.</returns>
        public virtual bool IsAccumulationSupported()
        {
            return false;
        }

        ///<summary>
        /// Indicates the latest version of the recorder.
        /// This is used during the asset upgrade process to determine if the asset needs to be upgraded.
        /// Derived classes should override this property to provide their own latest version.
        /// </summary>
        protected virtual int LatestVersion { get => 0; }

        /// <summary>
        /// Indicates the current version of this recorder.
        /// This is used during the asset upgrade process to determine if the asset needs to be upgraded.
        /// Derived classes should override this property to provide their own version.
        /// </summary>
        protected virtual int Version { get; set; } = 0;

        /// <summary>
        /// Unity calls this method before serializing the object.
        /// </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            OnBeforeSerialize();
        }

        /// <summary>
        /// Unity calls this method after de-serializing the object.
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (Version < LatestVersion)
            {
                OnUpgradeFromVersion(); //upgrade derived classes
            }

            OnAfterDeserialize();
        }

        void OnEnable()
        {
            OnValidateUpgrade();
        }

        void OnValidateUpgrade()
        {
            Version = LatestVersion;
        }

        /// <summary>
        /// Called before a RecorderSetting is serialized.
        /// </summary>
        protected virtual void OnBeforeSerialize() {}// Do not clean up since users can only override this

        /// <summary>
        /// Called after a RecorderSetting has been deserialized.
        /// </summary>
        protected virtual void OnAfterDeserialize() {} // Do not clean up since users can only override this

        /// <summary>
        /// Defines how to handle the upgrade of Recorder Settings created in a previous version according to their type.
        /// Unity automatically calls this method when loading a Recorder Setting (after deserialization) if its version is older than the current project version.
        /// </summary>
        protected virtual void OnUpgradeFromVersion() {}

        /// <summary>
        /// An interface used for classes that include Resolution information
        /// </summary>
        internal interface IResolutionUser
        {
            internal bool IsOutputResolutionContradictory
            {
                get;
                set;
            }

            internal int OutputWidth { get; }
            internal int OutputHeight { get; }

            internal Type ImageInputType { get; }
        }
    }
}
