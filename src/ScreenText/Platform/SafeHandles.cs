using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ScreenText.Platform;

/// <summary>
/// Безопасный обработчик для HDC (Device Context) с автоматическим освобождением.
/// </summary>
public sealed class SafeDCHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private readonly IntPtr _hWnd;
    private readonly bool _isMemoryDC;

    private SafeDCHandle(IntPtr handle, IntPtr hWnd, bool isMemoryDC) : base(true)
    {
        SetHandle(handle);
        _hWnd = hWnd;
        _isMemoryDC = isMemoryDC;
    }

    public static SafeDCHandle CreateScreenDC()
    {
        var hdc = NativeMethods.GetDC(IntPtr.Zero);
        if (hdc == IntPtr.Zero)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Unable to acquire screen DC");
        return new SafeDCHandle(hdc, IntPtr.Zero, false);
    }

    public static SafeDCHandle CreateMemoryDC(SafeDCHandle screenDC)
    {
        if (screenDC.IsInvalid || screenDC.IsClosed)
            throw new ArgumentException("Screen DC is invalid", nameof(screenDC));
        
        var hdc = NativeMethods.CreateCompatibleDC(screenDC.handle);
        if (hdc == IntPtr.Zero)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Unable to create compatible DC");
        return new SafeDCHandle(hdc, IntPtr.Zero, true);
    }

    protected override bool ReleaseHandle()
    {
        if (_isMemoryDC)
        {
            return NativeMethods.DeleteDC(handle);
        }
        else
        {
            return NativeMethods.ReleaseDC(_hWnd, handle) != 0;
        }
    }
}

/// <summary>
/// Безопасный обработчик для GDI-объектов (Bitmap, Pen, Brush и т.д.)
/// </summary>
public sealed class SafeGdiObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private SafeGdiObjectHandle() : base(true) { }

    public static SafeGdiObjectHandle FromHandle(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
            throw new ArgumentException("Handle cannot be zero", nameof(handle));
        return new SafeGdiObjectHandle { handle = handle };
    }

    protected override bool ReleaseHandle()
    {
        return NativeMethods.DeleteObject(handle);
    }
}
