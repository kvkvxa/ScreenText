using System.Drawing;
using System.Drawing.Imaging;
using ScreenText.Platform;

namespace ScreenText.Capture;

public sealed class ScreenCapture
{
    public Bitmap Capture(PhysicalRect rect)
    {
        if (rect.IsEmpty) throw new ScreenCaptureException("Capture rectangle must not be empty.");

        var screenDc = NativeMethods.GetDC(IntPtr.Zero);
        if (screenDc == IntPtr.Zero) throw new ScreenCaptureException("Unable to acquire screen DC.");
        var memoryDc = IntPtr.Zero;
        var bitmapHandle = IntPtr.Zero;
        var previousObject = IntPtr.Zero;
        try
        {
            memoryDc = NativeMethods.CreateCompatibleDC(screenDc);
            if (memoryDc == IntPtr.Zero) throw new ScreenCaptureException("Unable to create compatible DC.");
            var bitmapInfo = new NativeMethods.BITMAPINFO
            {
                Header = new NativeMethods.BITMAPINFOHEADER
                {
                    Size = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
                    Width = rect.Width,
                    Height = -rect.Height,
                    Planes = 1,
                    BitCount = 32,
                    Compression = 0
                }
            };
            bitmapHandle = NativeMethods.CreateDIBSection(screenDc, ref bitmapInfo, 0, out _, IntPtr.Zero, 0);
            if (bitmapHandle == IntPtr.Zero) throw new ScreenCaptureException("Unable to create DIB section.");
            previousObject = NativeMethods.SelectObject(memoryDc, bitmapHandle);
            if (previousObject == IntPtr.Zero) throw new ScreenCaptureException("Unable to select DIB section.");
            if (!NativeMethods.BitBlt(memoryDc, 0, 0, rect.Width, rect.Height, screenDc, rect.Left, rect.Top, NativeMethods.SRCCOPY | NativeMethods.CAPTUREBLT))
                throw new ScreenCaptureException("BitBlt failed.");

            using var image = Image.FromHbitmap(bitmapHandle);
            return new Bitmap(image, rect.Width, rect.Height);
        }
        finally
        {
            if (previousObject != IntPtr.Zero) NativeMethods.SelectObject(memoryDc, previousObject);
            if (bitmapHandle != IntPtr.Zero) NativeMethods.DeleteObject(bitmapHandle);
            if (memoryDc != IntPtr.Zero) NativeMethods.DeleteDC(memoryDc);
            NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }
}
