using System.Runtime.InteropServices;

namespace ScreenText.Platform;

public static class NativeMethods
{
    public const int WM_HOTKEY = 0x0312;
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint SRCCOPY = 0x00CC0020;
    public const uint CAPTUREBLT = 0x40000000;
    public const int SWP_NOACTIVATE = 0x0010;
    public const int SWP_SHOWWINDOW = 0x0040;
    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public static readonly IntPtr PerMonitorAwareV2 = new(-4);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO monitorInfo);

    [DllImport("user32.dll")]
    public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clipRect, MonitorEnumProc callback, IntPtr data);

    [DllImport("shcore.dll")]
    public static extern int GetDpiForMonitor(IntPtr hMonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern IntPtr CreateCompatibleDC(IntPtr hDc);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern bool DeleteDC(IntPtr hDc);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern IntPtr CreateDIBSection(IntPtr hDc, ref BITMAPINFO bitmapInfo, uint usage, out IntPtr bits, IntPtr section, uint offset);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern IntPtr SelectObject(IntPtr hDc, IntPtr hObject);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern bool BitBlt(IntPtr destinationDc, int x, int y, int width, int height, IntPtr sourceDc, int sourceX, int sourceY, uint rasterOperation);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MONITORINFO
    {
        public int CbSize;
        public RECT Monitor;
        public RECT WorkArea;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    public enum MonitorDpiType { Effective = 0, Angular = 1, Raw = 2 }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFO
    {
        public BITMAPINFOHEADER Header;
        public RGBQUAD Color;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RGBQUAD { public byte Blue; public byte Green; public byte Red; public byte Reserved; }
}
