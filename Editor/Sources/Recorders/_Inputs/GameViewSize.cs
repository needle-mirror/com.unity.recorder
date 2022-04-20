#if UNITY_2022_2_OR_NEWER
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Recorder.Editor.Tests")]

namespace UnityEditor.Recorder.Input
{
    static class GameViewSize
    {
        public static bool IsMainPlayViewGameView()
        {
            return PlayModeWindow.GetViewType() == PlayModeWindow.PlayModeViewTypes.GameView;
        }

        public static void SwapMainPlayViewToGameView()
        {
            if (IsMainPlayViewGameView())
                return;

            PlayModeWindow.SetViewType(PlayModeWindow.PlayModeViewTypes.GameView);
        }

        public static void DisableMaxOnPlay()
        {
            PlayModeWindow.SetPlayModeFocused(true);
        }

        public static void GetGameRenderSize(out uint width, out uint height)
        {
            PlayModeWindow.GetRenderingResolution(out width, out height);
        }

        public static void SetCustomSize(int width, int height)
        {
            PlayModeWindow.SetCustomRenderingResolution((uint)width, (uint)height, "Recording Resolution");
        }
    }
}
#else


using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("Unity.Recorder.Editor.Tests")]

namespace UnityEditor.Recorder.Input
{
    static class GameViewSize
    {
        static object s_InitialSizeObj;
        const int miscSize = 1; // Used when no main GameView exists (ex: batchmode)

        static Type s_PlayModeViewType = Type.GetType("UnityEditor.PlayModeView,UnityEditor");
        static string s_GetGameViewFuncName = "GetMainPlayModeView";
        static string s_RecordingResolutionBaseName = "Recording Resolution";

        public static bool IsMainPlayViewGameView()
        {
            var gameView = GetMainPlayModeView();
            if (gameView == null)
                return true;
            return gameView.GetType().Name == "GameView";
        }

        public static void SwapMainPlayViewToGameView()
        {
            if (IsMainPlayViewGameView())
                return;

            var gameView = GetMainPlayModeView();
            if (gameView == null)
                return;

            var swapMainWindow = gameView.GetType().GetMethod("SwapMainWindow",  BindingFlags.NonPublic | BindingFlags.Instance);
            var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            swapMainWindow.Invoke(gameView, new object[] { gameViewType });
        }

        public static void DisableMaxOnPlay()
        {
            var gameView = GetMainPlayModeView();
            if (gameView == null)
                return;

            var getMaximizeOnPlayMethod = gameView.GetType().GetMethod("get_maximizeOnPlay",  BindingFlags.Public | BindingFlags.Instance);

            bool maximizeOnPlay = false;
            if (getMaximizeOnPlayMethod != null)
            {
                maximizeOnPlay = (bool)getMaximizeOnPlayMethod.Invoke(gameView, new object[] {});
                if (maximizeOnPlay)
                {
                    Debug.LogWarning("'Maximize on Play' not compatible with recorder: disabling it!");
                    var m = gameView.GetType().GetMethod("set_maximizeOnPlay",
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    m.Invoke(gameView, new object[] {false});
                }
            }
        }

        public static void GetGameRenderSize(out int width, out int height)
        {
            var gameView = GetMainPlayModeView();

            if (gameView == null)
            {
                width = height = miscSize;
                return;
            }

            var prop = gameView.GetType().GetProperty("targetSize", BindingFlags.NonPublic | BindingFlags.Instance);
            var size = (Vector2)prop.GetValue(gameView, new object[] {});
            width = (int)size.x;
            height = (int)size.y;
        }

        public static void SetCustomSize(int width, int height)
        {
            var sizeObj = FindRecorderSizeObj();
            if (sizeObj != null)
            {
                var setWidth = sizeObj.GetType().GetProperty("width");
                var setHeight = sizeObj.GetType().GetProperty("height");
                setWidth.SetValue(sizeObj, width);
                setHeight.SetValue(sizeObj, height);
            }
            else
            {
                sizeObj = AddSize(width, height);
            }

            SelectSize(sizeObj);
        }

        static object AddSize(int width, int height)
        {
            var sizeObj = NewSizeObj(width, height);

            var group = Group();
            var obj = group.GetType().GetMethod("AddCustomSize", BindingFlags.Public | BindingFlags.Instance);
            obj.Invoke(group, new[] {sizeObj});

            return sizeObj;
        }

        static void SelectSize(object size)
        {
            if (size == null)
                return;
            var index = IndexOf(size);

            var gameView = GetMainPlayModeView();
            if (gameView == null)
                return;
            var obj = gameView.GetType().GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.Instance);
            if (obj == null)
                return;
            obj.Invoke(gameView, new[] { index, size });
        }

        static object Group()
        {
            var T = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
            var sizes = T.BaseType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = sizes.GetValue(null, new object[] {});

            var currentGroup = instance.GetType().GetProperty("currentGroup", BindingFlags.Public | BindingFlags.Instance);
            var group = currentGroup.GetValue(instance, new object[] {});
            return group;
        }

        static EditorWindow GetMainPlayModeView()
        {
            var getMainGameView = s_PlayModeViewType.GetMethod(s_GetGameViewFuncName, BindingFlags.NonPublic | BindingFlags.Static);
            if (getMainGameView == null)
            {
                Debug.LogError(string.Format("Can't find the main Game View : {0} function was not found in {1} type ! Did API change ?",
                    s_GetGameViewFuncName, s_PlayModeViewType));
                return null;
            }
            var res = getMainGameView.Invoke(null, null);
            return (EditorWindow)res;
        }

        static object FindRecorderSizeObj()
        {
            var group = Group();

            var customs = group.GetType().GetField("m_Custom", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(group);

            var itr = (IEnumerator)customs.GetType().GetMethod("GetEnumerator").Invoke(customs, new object[] {});
            while (itr.MoveNext())
            {
                var txt = (string)itr.Current.GetType().GetField("m_BaseText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(itr.Current);
                if (txt == s_RecordingResolutionBaseName)
                    return itr.Current;
            }

            return null;
        }

        static int IndexOf(object sizeObj)
        {
            var group = Group();
            var method = group.GetType().GetMethod("IndexOf", BindingFlags.Public | BindingFlags.Instance);
            var index = (int)method.Invoke(group, new[] {sizeObj});

            var builtinList = group.GetType().GetField("m_Builtin", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(group);

            method = builtinList.GetType().GetMethod("Contains");
            if ((bool)method.Invoke(builtinList, new[] { sizeObj }))
                return index;

            method = group.GetType().GetMethod("GetBuiltinCount");
            index += (int)method.Invoke(group, new object[] {});

            return index;
        }

        static object NewSizeObj(int width, int height)
        {
            var T = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
            var tt = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");

            var c = T.GetConstructor(new[] {tt, typeof(int), typeof(int), typeof(string)});
            var sizeObj = c.Invoke(new object[] {1, width, height,  s_RecordingResolutionBaseName});
            return sizeObj;
        }
    }
}

#endif
