using System.Drawing;
using System.Drawing.Imaging;
using ScreenText.Platform;

namespace ScreenText.Capture;

public sealed class ScreenCapture
{
    public Bitmap Capture(PhysicalRect rect)
    {
        if (rect.IsEmpty) throw new ScreenCaptureException("Capture rectangle must not be empty.");

        using var screenDC = SafeDCHandle.CreateScreenDC();
        using var memoryDC = SafeDCHandle.CreateMemoryDC(screenDC);
        
        IntPtr bitmapHandle = IntPtr.Zero;
        IntPtr previousObject = IntPtr.Zero;
        try
        {
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
            
            bitmapHandle = NativeMethods.CreateDIBSection(screenDC.HandlePtr, ref bitmapInfo, 0, out _, IntPtr.Zero, 0);
            if (bitmapHandle == IntPtr.Zero) 
                throw new ScreenCaptureException("Unable to create DIB section.");
            
            previousObject = NativeMethods.SelectObject(memoryDC.HandlePtr, bitmapHandle);
            if (previousObject == IntPtr.Zero) 
                throw new ScreenCaptureException("Unable to select DIB section.");
            
            if (!NativeMethods.BitBlt(memoryDC.HandlePtr, 0, 0, rect.Width, rect.Height, screenDC.HandlePtr, rect.Left, rect.Top, NativeMethods.SRCCOPY | NativeMethods.CAPTUREBLT))
                throw new ScreenCaptureException("BitBlt failed.");

            using var image = Image.FromHbitmap(bitmapHandle);
            return new Bitmap(image, rect.Width, rect.Height);
        }
        finally
        {
            if (previousObject != IntPtr.Zero) 
                NativeMethods.SelectObject(memoryDC.HandlePtr, previousObject);
            if (bitmapHandle != IntPtr.Zero) 
                NativeMethods.DeleteObject(bitmapHandle);
        }
    }
}
