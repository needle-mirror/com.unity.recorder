using System;
using System.Runtime.InteropServices;
using System.Text;

struct GIFWrapperInfo
{
#if UNITY_EDITOR_WIN
    public const string LibraryPath = "GIFWrapper";
#elif UNITY_EDITOR_OSX
    public const string LibraryPath = "libGIFWrapper.bundle";
#else
    public const string LibraryPath = "libGIFWrapper";
#endif
}

internal struct GIFWrapper
{
    [DllImport(GIFWrapperInfo.LibraryPath)]
    public static extern IntPtr Create(string sFileName, int width, int height, bool loopForever, bool constantFrameRate, bool fast, float fps, int quality);
    [DllImport(GIFWrapperInfo.LibraryPath)]
    public static extern unsafe bool AddVideoFrame(IntPtr pEncoder, void* pixels, float timeMilliseconds);
    [DllImport(GIFWrapperInfo.LibraryPath)]
    public static extern bool Close(IntPtr pEncoder);
}
