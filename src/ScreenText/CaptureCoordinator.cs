using System.Drawing;
using ScreenText.Ocr;
using ScreenText.Platform;
using ScreenText.Settings;

namespace ScreenText;

public enum CaptureState { Idle, Selecting, Recognizing, WritingClipboard }
public enum CaptureOperation { Text, Image }

public sealed class CaptureCoordinator : IDisposable
{
    private readonly Capture.SelectionOverlayManager _selectionManager;
    private readonly Capture.ScreenCapture _screenCapture;
    private readonly OcrService _ocrService;
    private readonly ClipboardService _clipboardService;
    private readonly NotificationService _notificationService;
    private readonly Func<AppSettings> _settingsProvider;
    private readonly CancellationTokenSource _shutdown = new();
    private int _active;
    private bool _disposed;
    private Task? _activeTask;

    public CaptureState State { get; private set; } = CaptureState.Idle;
    public CaptureCoordinator(
        Capture.SelectionOverlayManager selectionManager,
        Capture.ScreenCapture screenCapture,
        OcrService ocrService,
        ClipboardService clipboardService,
        NotificationService notificationService,
        Func<AppSettings> settingsProvider)
    {
        _selectionManager = selectionManager;
        _screenCapture = screenCapture;
        _ocrService = ocrService;
        _clipboardService = clipboardService;
        _notificationService = notificationService;
        _settingsProvider = settingsProvider;
    }

    public void StartCapture()
    {
        StartOperation(CaptureOperation.Text);
    }

    public void StartImageCapture()
    {
        StartOperation(CaptureOperation.Image);
    }

    private void StartOperation(CaptureOperation operation)
    {
        if (_disposed) return;
        if (Interlocked.Exchange(ref _active, 1) != 0) return;
        _activeTask = RunCaptureAsync(operation);
    }

    private async Task RunCaptureAsync(CaptureOperation operation)
    {
        try
        {
            State = CaptureState.Selecting;
            var selectedRegion = await _selectionManager.SelectRegionAsync();
            if (selectedRegion is null) return;
            if (!_selectionManager.IsSelectionOnCurrentTopology(selectedRegion.Value))
                throw new Capture.ScreenCaptureException("Monitor topology changed during selection.");
            using var bitmap = _screenCapture.Capture(selectedRegion.Value);
            if (operation == CaptureOperation.Image)
            {
                State = CaptureState.WritingClipboard;
                await _clipboardService.SetImageAsync(bitmap, _shutdown.Token);
                var imageSettings = _settingsProvider();
                if (imageSettings.ShowNotification) _notificationService.ShowInfo("Screenshot copied to clipboard.");
                return;
            }

            State = CaptureState.Recognizing;
            var settings = _settingsProvider();
            var language = OcrLanguageParser.Parse(settings.Language);
            var result = await _ocrService.RecognizeAsync(bitmap, language, _shutdown.Token);
            if (string.IsNullOrWhiteSpace(result.Text))
            {
                if (settings.ShowNotification) _notificationService.ShowWarning("No text was detected.");
                return;
            }
            if (settings.AutoCopy)
            {
                State = CaptureState.WritingClipboard;
                await _clipboardService.SetTextAsync(result.Text, _shutdown.Token);
                if (settings.ShowNotification) _notificationService.ShowInfo("Text copied to clipboard.");
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
        }
        catch (OcrModelMissingException exception)
        {
            _notificationService.ShowWarning(exception.ModelCode == "osd"
                ? "Automatic language detection is unavailable. Add the local osd.traineddata model."
                : "OCR model is missing. Reinstall the bundled models.");
        }
        catch (OcrModelIntegrityException)
        {
            _notificationService.ShowWarning("OCR model integrity check failed. Reinstall the bundled models.");
        }
        catch (OcrUnsupportedScriptException)
        {
            _notificationService.ShowWarning("Unsupported writing system detected. Add its local Tesseract model or choose a supported language.");
        }
        catch (Capture.ScreenCaptureException)
        {
            _notificationService.ShowWarning("Screenshot capture failed. The application is ready for another capture.");
        }
        catch (ClipboardUnavailableException)
        {
            _notificationService.ShowWarning("Clipboard is unavailable. Close other clipboard tools and try again.");
        }
        catch (Exception)
        {
            _notificationService.ShowWarning(operation == CaptureOperation.Image
                ? "Screenshot copy failed. The application is ready for another capture."
                : "OCR failed. The application is ready for another capture.");
        }
        finally
        {
            State = CaptureState.Idle;
            Volatile.Write(ref _active, 0);
        }
    }

    public void Cancel() => _selectionManager.Cancel();

    public async Task StopAsync()
    {
        if (_disposed) return;
        _shutdown.Cancel();
        _selectionManager.Cancel();
        var activeTask = _activeTask;
        if (activeTask is not null) await activeTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shutdown.Cancel();
        _selectionManager.Cancel();
        State = CaptureState.Idle;
        Volatile.Write(ref _active, 0);
        _shutdown.Dispose();
    }
}
