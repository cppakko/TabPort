using System;
using System.Runtime.InteropServices;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.TabPort.util;

public static partial class NativeUtils
{
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr hWnd);

    public static void DoShowWindow(IntPtr hWnd, BrowserTab tab)
    {
        var result = SetForegroundWindow(hWnd);
        if (result) return;
        var error = Marshal.GetLastWin32Error();
        Log.Info($"[DoShowWindow] Failed to SetForegroundWindow: {tab.Title} ({hWnd.ToString()}) " + error,
            typeof(NativeUtils));
    }
}