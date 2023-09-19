//#define DEBUG_ANALYTICS
using System.Linq;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering;
[assembly: InternalsVisibleTo("Unity.Recorder.Tests")]

namespace UnityEditor.Recorder
{
    static class RecorderAnalytics
    {
        // The 2 dictionaries help the analytics correlate the data between the start and end event without scattering data all over the RecorderControllers/Settings.
        static readonly Dictionary<RecorderController, string> controller2guid = new Dictionary<RecorderController, string>(); // RecorderWindow
        static readonly Dictionary<RecorderSettings, string> Settings2Guid = new Dictionary<RecorderSettings, string>(); // From the timeline

        const int maxEventsPerHour = 1000;
        const int maxNumberOfElements = 1000;

        const string vendorKey = "unity.recorder";
        const string startEventName = "recorder_session_start";
        const string completeEventName = "recorder_session_complete";

        const int startEventVersion = 2;
        const int completeEventVersion = 1;

#if UNITY_2023_2_OR_NEWER

        [AnalyticInfo(eventName: startEventName, vendorKey: vendorKey, version: startEventVersion)]
        internal class SessionStartAnalytic : IAnalytic
        {
            private RecorderSessionStartEvent? data = null;
            public SessionStartAnalytic(RecorderSessionStartEvent data)
            {
                this.data = data;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = this.data;
                return data != null;
            }
        }
        [Serializable]

        internal struct RecorderSessionStartEvent : IAnalytic.IData
#else
        internal struct RecorderSessionStartEvent
#endif
        {
            public string recorder_session_guid;
            public bool exit_play_mode;
            public string recording_mode;
            public string playback_mode;
            public float target_fps;
            public bool cap_fps;
            public string triggered_by;
            public string render_pipeline;

            public List<RecorderInfo> recorder_info;
            public List<AnimationRecorderInfo> animation_recorder_info;
            public List<ImageRecorderInfo> image_recorder_info;
            public List<MovieRecorderInfo> movie_recorder_info;
        }

#if UNITY_2023_2_OR_NEWER

        [AnalyticInfo(eventName: completeEventName, vendorKey: vendorKey, version: completeEventVersion)]
        internal class SessionEndAnalytic : IAnalytic
        {
            private RecorderSessionEndEvent? data = null;
            public SessionEndAnalytic(RecorderSessionEndEvent data)
            {
                this.data = data;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = this.data;
                return data != null;
            }
        }
        [Serializable]
        internal struct RecorderSessionEndEvent : IAnalytic.IData
#else
        internal struct RecorderSessionEndEvent
#endif
        {
            public string recorder_session_guid;
            public string outcome;
            public int elapsed_ms;
            public int  frames_recorded;

            internal enum Outcome
            {
                Complete,
                UserStopped,
                Error
            }
        }


        [Serializable]
        internal class RecorderInfo
        {
            public string type;
            public string record_guid;
            public bool enabled;

            public static void FromRecorder(RecorderSettings r, RecorderInfo recorderInfo)
            {
                var errors = new List<string>();
                r.GetErrors(errors);

                recorderInfo.type = RecordersInventory.GetRecorderInfo(r.GetType()).recorderType.FullName;
                recorderInfo.record_guid = r.GetInstanceID().ToString();
                recorderInfo.enabled = r.Enabled;
            }
        }

        [Serializable]
        internal class AnimationRecorderInfo : RecorderInfo
        {
            public bool record_hierarchy;
            public bool  clamped_tangets;
            public string anim_compression;
            public static AnimationRecorderInfo FromRecorder(AnimationRecorderSettings r)
            {
                var ret = new AnimationRecorderInfo
                {
                    type = RecordersInventory.GetRecorderInfo(r.GetType()).recorderType.FullName,
                    record_guid = r.GetInstanceID().ToString(),
                    anim_compression =  r.AnimationInputSettings.SimplyCurves.ConvertToString(),
                    record_hierarchy = r.AnimationInputSettings.Recursive,
                    clamped_tangets = r.AnimationInputSettings.ClampedTangents
                };

                RecorderInfo.FromRecorder(r, ret);

                return ret;
            }
        }


        [Serializable]
        internal class ImageRecorderInfo : RecorderInfo
        {
            public string source;
            public int output_resolution_w;
            public int output_resolution_h;
            public string media_format;
            public string color_space;
            public AccumulationInfo accumulation_info;

            public static ImageRecorderInfo FromRecorder(ImageRecorderSettings r)
            {
                var ret = new ImageRecorderInfo()
                {
                    type = RecordersInventory.GetRecorderInfo(r.GetType()).recorderType.FullName,
                    record_guid = r.GetInstanceID().ToString(),
                    color_space = r.OutputFormat == ImageRecorderSettings.ImageRecorderOutputFormat.EXR ? r.OutputColorSpace.ConvertToString() : null,
                    media_format = r.OutputFormat.ConvertToString(),
                    output_resolution_h = r.imageInputSettings.OutputHeight,
                    output_resolution_w = r.imageInputSettings.OutputWidth,
                    source = r.imageInputSettings.ConvertToString(),
                    accumulation_info = AccumulationInfo.FromRecorder(r)
                };
                RecorderInfo.FromRecorder(r, ret);

                return ret;
            }
        }

        [Serializable]
        internal class MovieRecorderInfo : RecorderInfo
        {
            public string source;
            public int output_resolution_w;
            public int output_resolution_h;
            public string media_format;
            public bool include_audio;
            public string codec_format;
            public string quality;
            public AccumulationInfo accumulation_info;
            public string encoder_type;
            public float target_bitrate;
            public uint keyframe_distance;
            public string encoding_profile;
            public uint b_frames;

            public static MovieRecorderInfo FromRecorder(MovieRecorderSettings r)
            {
                var ret = new MovieRecorderInfo()
                {
                    type = RecordersInventory.GetRecorderInfo(r.GetType()).recorderType.FullName,
                    record_guid = r.GetInstanceID().ToString(),
                    output_resolution_h = r.ImageInputSettings.OutputHeight,
                    output_resolution_w = r.ImageInputSettings.OutputWidth,
                    source = r.ImageInputSettings.ConvertToString(),
                    include_audio = r.CaptureAudio,
                    accumulation_info = AccumulationInfo.FromRecorder(r),
                    encoder_type = r.EncoderSettings != null
                        ? r.EncoderSettings.GetType().FullName
                        : ""
                };

                if (r.EncoderSettings == null) return ret;

                switch (r.EncoderSettings)
                {
                    case ProResEncoderSettings proResEncoderSettings:
                        ret.codec_format =
                            proResEncoderSettings.Format.ConvertToString();

                        break;
                    case CoreEncoderSettings coreEncoderSettings:
                    {
                        ret.media_format = coreEncoderSettings.Codec.ConvertToString();
                        ret.quality = coreEncoderSettings.EncodingQuality
                            .ConvertToString();
                        if (coreEncoderSettings.EncodingQuality == CoreEncoderSettings.VideoEncodingQuality.Custom)
                        {
                            ret.target_bitrate = coreEncoderSettings.TargetBitRate;
                            ret.keyframe_distance = coreEncoderSettings.Codec == CoreEncoderSettings.OutputCodec.WEBM
                                ? coreEncoderSettings.KeyframeDistance
                                : coreEncoderSettings.GopSize;
                            if (coreEncoderSettings.Codec == CoreEncoderSettings.OutputCodec.MP4)
                            {
                                ret.encoding_profile = coreEncoderSettings.EncodingProfile.ConvertToString();
                                ret.b_frames = coreEncoderSettings.numConsecutiveBFrames;
                            }
                        }

                        break;
                    }
                }

                ret.media_format = r.EncoderSettings.Extension;

                RecorderInfo.FromRecorder(r, ret);
                return ret;
            }
        }

        [Serializable]
        internal struct AccumulationInfo
        {
            public bool enabled;
            public int num_samples;
            public string shutter_profile_range;
            public float shutter_begin;
            public float shutter_end;
            public bool subpixel_jitter;
            public float shutter_interval;

            public static AccumulationInfo FromRecorder(RecorderSettings r)
            {
                if (r is not IAccumulation settings)
                    return default;
                var aSettings = settings.GetAccumulationSettings();
                return new AccumulationInfo
                {
                    enabled = aSettings.CaptureAccumulation,
                    num_samples = aSettings.Samples,
                    subpixel_jitter = aSettings.UseSubPixelJitter,
                    shutter_profile_range = aSettings.ShutterType.ToString(),
                    shutter_interval = aSettings.ShutterInterval,
                    shutter_begin = aSettings.ShutterType == AccumulationSettings.ShutterProfileType.Range
                        ? aSettings.ShutterFullyOpen
                        : 0,
                    shutter_end = aSettings.ShutterType == AccumulationSettings.ShutterProfileType.Range
                        ? aSettings.ShutterBeginsClosing
                        : 0
                };
            }
        }

        // Used by the RecorderWindow
        public static void SendStartEvent(RecorderController controller)
        {
            if (!EditorAnalytics.enabled)
                return;
#if UNITY_2023_2_OR_NEWER
            EditorAnalytics.SendAnalytic(new SessionStartAnalytic(CreateSessionStartEvent(controller)));
#else
            EditorAnalytics.RegisterEventWithLimit(startEventName, maxEventsPerHour, maxNumberOfElements, vendorKey, startEventVersion);
            var data = CreateSessionStartEvent(controller);
            EditorAnalytics.SendEventWithLimit(startEventName, data, startEventVersion);
#endif
        }

        public static void SendStopEvent(RecorderController controller, bool error)
        {
            if (!EditorAnalytics.enabled)
                return;
#if UNITY_2023_2_OR_NEWER
            EditorAnalytics.SendAnalytic(new SessionEndAnalytic(CreateStopEvent(controller, error)));
#else
            EditorAnalytics.RegisterEventWithLimit(completeEventName, maxEventsPerHour, maxNumberOfElements, vendorKey, completeEventVersion);

            var data = CreateStopEvent(controller, error);
            EditorAnalytics.SendEventWithLimit(completeEventName, data, completeEventVersion);
#endif
        }

        // Used by the Timeline
        public static void SendStartEvent(RecordingSession session)
        {
            if (!EditorAnalytics.enabled)
                return;
#if UNITY_2023_2_OR_NEWER
            EditorAnalytics.SendAnalytic(new SessionStartAnalytic(CreateSessionStartEvent(session)));
#else
            EditorAnalytics.RegisterEventWithLimit(startEventName, maxEventsPerHour, maxNumberOfElements, vendorKey, startEventVersion);
            var data = CreateSessionStartEvent(session);
            // Send the data to the database
            EditorAnalytics.SendEventWithLimit(startEventName, data, startEventVersion);
#endif
        }

        public static void SendStopEvent(RecordingSession session, bool error, bool complete)
        {
            if (!EditorAnalytics.enabled)
                return;
#if UNITY_2023_2_OR_NEWER
            EditorAnalytics.SendAnalytic(new SessionEndAnalytic(CreateStopEvent(session, error, complete)));
#else
            EditorAnalytics.RegisterEventWithLimit(completeEventName, maxEventsPerHour, maxNumberOfElements, vendorKey, completeEventVersion);

            var data = CreateStopEvent(session, error, complete);

            // Send the data to the database
            EditorAnalytics.SendEventWithLimit(completeEventName, data, completeEventVersion);
#endif
        }

        internal static RecorderSessionEndEvent CreateStopEvent(RecorderController controller, bool error)
        {
            if (!controller2guid.TryGetValue(controller, out var guid))
            {
                return new RecorderSessionEndEvent(); // Should never happen
            }

            controller2guid.Remove(controller);
            var session = controller.m_RecordingSessions.FirstOrDefault(x => x.settings.Enabled);

            RecorderSessionEndEvent data;

            if (session == null)
            {
                data = new RecorderSessionEndEvent {outcome = RecorderSessionEndEvent.Outcome.Error.ConvertToString()};
            }
            else
            {
                data = new RecorderSessionEndEvent
                {
                    recorder_session_guid = guid,
                    elapsed_ms = Mathf.Max(0, (int)(session.currentFrameStartTS * 1000)),
                    frames_recorded = session.recorder.RecordedFramesCount,
                    outcome = (error ? RecorderSessionEndEvent.Outcome.Error : GetOutcome(session)).ConvertToString()
                };
            }
#if DEBUG_ANALYTICS
            var json = JsonUtility.ToJson(data, prettyPrint: true);
            Debug.Log(json);
#endif

            return data;
        }

        static RecorderSessionEndEvent CreateStopEvent(RecordingSession session, bool error, bool complete)
        {
            if (!Settings2Guid.TryGetValue(session.settings, out var guid))
            {
                return new RecorderSessionEndEvent(); // Should never happen
            }

            Settings2Guid.Remove(session.settings);

            RecorderSessionEndEvent data;

            var outcome = error ? RecorderSessionEndEvent.Outcome.Error : (complete
                ? RecorderSessionEndEvent.Outcome.Complete
                : RecorderSessionEndEvent.Outcome.UserStopped);
            data = new RecorderSessionEndEvent
            {
                recorder_session_guid = guid,
                elapsed_ms = (int)((session.currentFrameStartTS) * 1000),
                frames_recorded = session.recorder.RecordedFramesCount,
                outcome = outcome.ConvertToString()
            };

#if DEBUG_ANALYTICS
            var json = JsonUtility.ToJson(data, prettyPrint: true);
            Debug.Log(json);
#endif
            return data;
        }

        static RecorderSessionEndEvent.Outcome GetOutcome(this RecordingSession session)
        {
            if (session == null)
                return RecorderSessionEndEvent.Outcome.Error;

            if (session.settings.RecordMode == RecordMode.TimeInterval && session.currentFrameStartTS < session.settings.EndTime ||
                session.settings.RecordMode == RecordMode.FrameInterval && session.frameIndex < session.settings.EndFrame)
            {
                return RecorderSessionEndEvent.Outcome.UserStopped;
            }

            return RecorderSessionEndEvent.Outcome.Complete;
        }

        static RecorderSessionStartEvent CreateSessionStartEvent(RecordingSession session)
        {
            var guid = GUID.Generate().ToString();
            Settings2Guid[session.settings] = guid;

            var data = new RecorderSessionStartEvent
            {
                recorder_session_guid = guid,
                exit_play_mode = false, // not available in timeline
                target_fps = session.settings.FrameRate,
                triggered_by = "timeline",
                render_pipeline = GetCurrentRenderPipeline(),
            };

            GetSpecificRecorderInfos(
                new[] {session.recorder.settings},
                out data.animation_recorder_info,
                out data.image_recorder_info,
                out data.movie_recorder_info,
                out data.recorder_info);
#if DEBUG_ANALYTICS
            var json = JsonUtility.ToJson(data, prettyPrint: true);
            Debug.Log(json);
#endif

            return data;
        }

        internal static RecorderSessionStartEvent CreateSessionStartEvent(RecorderController controller)
        {
            var guid = GUID.Generate().ToString();
            controller2guid[controller] = guid;

            var controllerSettings = controller.Settings;

            var data = new RecorderSessionStartEvent
            {
                recorder_session_guid = guid,
                exit_play_mode = controllerSettings.ExitPlayMode,
                recording_mode = controllerSettings.RecordMode.ConvertToString(),
                playback_mode = controllerSettings.FrameRatePlayback.ConvertToString(),
                target_fps = controllerSettings.FrameRate,
                cap_fps = controllerSettings.CapFrameRate,
                triggered_by = "recorder",
                render_pipeline = GetCurrentRenderPipeline(),
            };

            GetSpecificRecorderInfos(
                controller.Settings.RecorderSettings,
                out data.animation_recorder_info,
                out data.image_recorder_info,
                out data.movie_recorder_info,
                out data.recorder_info);

#if DEBUG_ANALYTICS
            var json = JsonUtility.ToJson(data, prettyPrint: true);
            Debug.Log(json);
#endif
            return data;
        }

        static void GetSpecificRecorderInfos(IEnumerable<RecorderSettings> recorders, out List<AnimationRecorderInfo> anim,
            out List<ImageRecorderInfo> image, out List<MovieRecorderInfo> movie, out List<RecorderInfo> recorder)
        {
            anim = new List<AnimationRecorderInfo>();
            image = new List<ImageRecorderInfo>();
            movie = new List<MovieRecorderInfo>();
            recorder = new List<RecorderInfo>();

            foreach (var reco in recorders)
            {
                switch (reco)
                {
                    case AnimationRecorderSettings r:
                        anim.Add(AnimationRecorderInfo.FromRecorder(r));
                        break;
                    case ImageRecorderSettings r:
                        image.Add(ImageRecorderInfo.FromRecorder(r));
                        break;
                    case MovieRecorderSettings r:
                        movie.Add(MovieRecorderInfo.FromRecorder(r));
                        break;
                    default:
                        var ri = new RecorderInfo();
                        RecorderInfo.FromRecorder(reco, ri);
                        recorder.Add(ri);
                        break;
                }
            }

            if (anim.Count == 0)
                anim = null;
            if (image.Count == 0)
                image = null;
            if (movie.Count == 0)
                movie = null;
            if (recorder.Count == 0)
                recorder = null;
        }

        internal static string ConvertToString<T>(this T e) where T : Enum
        {
            return Enum.GetName(typeof(T), e).ToSnakeCase();
        }

        static string ToSnakeCase(this string str)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < str.Length - 1; ++i)
            {
                var ch = str[i];
                var nCh = str[i + 1];
                if (char.IsUpper(ch) && char.IsLower(nCh))
                {
                    sb.Append("_");
                }

                sb.Append(ch.ToString().ToLower());
            }

            sb.Append(str[str.Length - 1].ToString().ToLower());

            return sb.ToString().TrimStart('_');
        }

        static string ConvertToString(this ImageInputSettings i)
        {
            switch (i)
            {
                case GameViewInputSettings _:
                    return "game_view";
                case CameraInputSettings _:
                    return "target_camera";
                case Camera360InputSettings _:
                    return "view_360";
                case RenderTextureInputSettings _:
                    return "texture_asset";
                case RenderTextureSamplerSettings _:
                    return "texture_sampling";
                default:
                    return "unknown";
            }
        }

        static string GetCurrentRenderPipeline()
        {
            return GraphicsSettings.currentRenderPipeline == null ? "legacy" : GraphicsSettings.currentRenderPipeline.GetType().FullName;
        }
    }
}
