using ScreenText.Platform;
using System.Runtime.InteropServices;

namespace ScreenText.Capture;

public sealed class MonitorService
{
    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();
        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (handle, _, _, _) =>
        {
            var info = new NativeMethods.MONITORINFO { CbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
            if (NativeMethods.GetMonitorInfo(handle, ref info))
            {
                var dpiResult = NativeMethods.GetDpiForMonitor(handle, NativeMethods.MonitorDpiType.Effective, out var dpiX, out var dpiY);
                if (dpiResult != 0) { dpiX = dpiY = 96; }
                monitors.Add(new MonitorInfo(handle,
                    ToPhysicalRect(info.Monitor), ToPhysicalRect(info.WorkArea), dpiX, dpiY));
            }
            return true;
        }, IntPtr.Zero);
        return monitors;
    }

    private static PhysicalRect ToPhysicalRect(NativeMethods.RECT rect) => new(rect.Left, rect.Top, rect.Right, rect.Bottom);
}
