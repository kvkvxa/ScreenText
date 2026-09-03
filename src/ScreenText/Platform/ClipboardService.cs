using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfClipboard = System.Windows.Clipboard;

namespace ScreenText.Platform;

public sealed class ClipboardService
{
    private readonly Dispatcher _dispatcher;
    private const int MaxAttempts = 3;

    public ClipboardService(Dispatcher dispatcher) => _dispatcher = dispatcher;

    public async Task SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await _dispatcher.InvokeAsync(() =>
                {
                    WpfClipboard.SetText(text, System.Windows.TextDataFormat.UnicodeText);
                }, DispatcherPriority.Send);
                return;
            }
            catch (COMException exception)
            {
                if (attempt == MaxAttempts) throw new ClipboardUnavailableException("Clipboard remained unavailable after retries.", exception);
                await Task.Delay(40 * attempt, cancellationToken);
            }
            catch (ExternalException exception)
            {
                if (attempt == MaxAttempts) throw new ClipboardUnavailableException("Clipboard remained unavailable after retries.", exception);
                await Task.Delay(40 * attempt, cancellationToken);
            }
        }

        throw new InvalidOperationException("Unable to write text to the Windows clipboard.");
    }

    public async Task SetImageAsync(Bitmap bitmap, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await _dispatcher.InvokeAsync(() => SetImageOnUiThread(bitmap));
                return;
            }
            catch (COMException exception)
            {
                if (attempt == MaxAttempts) throw new ClipboardUnavailableException("Clipboard remained unavailable after retries.", exception);
                await Task.Delay(40 * attempt, cancellationToken);
            }
            catch (ExternalException exception)
            {
                if (attempt == MaxAttempts) throw new ClipboardUnavailableException("Clipboard remained unavailable after retries.", exception);
                await Task.Delay(40 * attempt, cancellationToken);
            }
        }

        throw new InvalidOperationException("Unable to write the image to the Windows clipboard.");
    }

    private static void SetImageOnUiThread(Bitmap bitmap)
    {
        var bitmapHandle = bitmap.GetHbitmap();
        try
        {
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                bitmapHandle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            bitmapSource.Freeze();
            var data = new System.Windows.DataObject();
            data.SetImage(bitmapSource);
            WpfClipboard.SetDataObject(data, true);
        }
        finally
        {
            NativeMethods.DeleteObject(bitmapHandle);
        }
    }
}
