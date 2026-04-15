using System.Runtime.InteropServices;

namespace ToDo.Services;

/// <summary>
/// Resolves monitor work areas using Win32 APIs — no WinForms dependency needed.
/// All coordinates are in physical pixels; callers must convert to WPF DIPs.
/// </summary>
internal static class MonitorHelper
{
    private const int MONITOR_DEFAULTTONEAREST = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
        public int Width  => Right  - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public uint   cbSize;
        public RECT   rcMonitor;   // Full monitor bounds (pixels)
        public RECT   rcWork;      // Work area — excludes taskbar (pixels)
        public uint   dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    /// <summary>
    /// Returns the work area (physical pixels) of the monitor that contains
    /// the given point. Falls back to the nearest monitor if the point is
    /// outside all monitors (e.g. after unplugging a screen).
    /// </summary>
    public static WorkArea GetWorkAreaForPoint(int x, int y)
    {
        var pt      = new POINT { X = x, Y = y };
        var hMon    = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
        var info    = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfo(hMon, ref info);

        return new WorkArea(
            info.rcWork.Left,
            info.rcWork.Top,
            info.rcWork.Width,
            info.rcWork.Height);
    }

    public readonly record struct WorkArea(int Left, int Top, int Width, int Height);
}