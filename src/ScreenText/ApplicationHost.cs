using System.Threading;
using System.Security;
using System.IO;
using ScreenText.Capture;
using ScreenText.Ocr;
using ScreenText.Platform;
using ScreenText.Settings;
using Tesseract;

namespace ScreenText;

public sealed class ApplicationHost : IDisposable
{
    private const string SingleInstanceMutexName = "ScreenText.SingleInstance";
    private const string ActivationEventName = "ScreenText.SingleInstance.Activate";

    private readonly Mutex _singleInstanceMutex = new(false, SingleInstanceMutexName);
    private readonly SettingsService _settings = new();
    private readonly LanguagePackService _languagePacks = new();
    private TrayService? _tray;
    private HotkeyService? _hotkey;
    private CaptureCoordinator? _captureCoordinator;
    private OcrService? _ocrService;
    private SettingsWindow? _settingsWindow;
    private Task? _ocrWarmupTask;
    private EventWaitHandle? _activationEvent;
    private System.Windows.Threading.DispatcherTimer? _activationPollTimer;
    private bool _ownsMutex;
    private bool _exitRequested;

    public bool Start()
    {
        _ownsMutex = _singleInstanceMutex.WaitOne(TimeSpan.Zero);
        if (!_ownsMutex)
        {
            SignalExistingInstance();
            _singleInstanceMutex.Dispose();
            return false;
        }

        InitializeActivationChannel();
        var settings = _settings.Load();
        NativeMethods.SetProcessDpiAwarenessContext(NativeMethods.PerMonitorAwareV2);
        _tray = new TrayService(
            onCapture: () => _captureCoordinator?.StartCapture(),
            onCaptureImage: () => _captureCoordinator?.StartImageCapture(),
            onSettings: ShowSettings,
            onExit: RequestExitAsync);
        _tray.Show();
        _ocrService = new OcrService();
        var clipboard = new ClipboardService(System.Windows.Application.Current.Dispatcher);
        var notifications = new NotificationService(_tray);
        var monitorService = new MonitorService();
        _captureCoordinator = new CaptureCoordinator(
            new SelectionOverlayManager(monitorService), new ScreenCapture(), _ocrService, clipboard, notifications, _settings.Load);
        _ocrWarmupTask = WarmUpOcrAsync(OcrLanguageParser.Parse(settings.Language));
        _hotkey = new HotkeyService();
        if (!_hotkey.Register(settings.Hotkey, _captureCoordinator.StartCapture))
        {
            _tray.ShowWarning("The configured global hotkey is unavailable or invalid.");
        }
        try
        {
            _settings.ApplyStartupSetting(settings.StartWithWindows);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or SecurityException or InvalidOperationException)
        {
            _tray.ShowWarning("Could not update the Windows startup setting.");
        }
        System.Windows.Application.Current.Dispatcher.BeginInvoke(
            new Action(() => _tray?.ShowInfo("ScreenText запущен. Используйте горячую клавишу или меню в трее.")),
            System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        return true;
    }

    private void ShowSettings()
    {
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings.Load(), _settings, _languagePacks, TryApplyHotkey);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private bool TryApplyHotkey(HotkeySettings hotkey)
    {
        if (_hotkey is null || _captureCoordinator is null) return false;
        return _hotkey.Register(hotkey, _captureCoordinator.StartCapture);
    }

    private async Task WarmUpOcrAsync(OcrLanguageSelection language)
    {
        try
        {
            await _ocrService!.WarmUpAsync(language);
        }
        catch (OcrModelMissingException exception) when (exception.ModelCode is not "osd")
        {
            _tray?.ShowWarning("The configured OCR language model is missing. Update the language models before capturing.");
        }
        catch (OcrModelIntegrityException)
        {
            _tray?.ShowWarning("OCR model integrity check failed. Reinstall the bundled models.");
        }
        catch (TesseractException)
        {
            _tray?.ShowWarning("The OCR engine could not be initialized. Try capturing again or reinstall the application.");
        }
        catch (ObjectDisposedException)
        {
            // Shutdown raced with the background warm-up; no user-facing warning is needed.
        }
    }

    private void InitializeActivationChannel()
    {
        try
        {
            _activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName);
            _activationPollTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _activationPollTimer.Tick += ActivationPollTimerOnTick;
            _activationPollTimer.Start();
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
        {
            _activationEvent?.Dispose();
            _activationEvent = null;
        }
    }

    private void ActivationPollTimerOnTick(object? sender, EventArgs e)
    {
        if (_activationEvent?.WaitOne(0) == true) ShowSettings();
    }

    private static void SignalExistingInstance()
    {
        try
        {
            using var activationEvent = EventWaitHandle.OpenExisting(ActivationEventName);
            activationEvent.Set();
        }
        catch (Exception exception) when (exception is WaitHandleCannotBeOpenedException or UnauthorizedAccessException or IOException)
        {
            // The existing instance may be shutting down or the activation channel may be unavailable.
        }
    }

    private async Task RequestExitAsync()
    {
        if (_exitRequested) return;
        _exitRequested = true;
        try
        {
            if (_captureCoordinator is not null) await _captureCoordinator.StopAsync();
            if (_ocrWarmupTask is not null) await _ocrWarmupTask;
        }
        catch (Exception)
        {
            _tray?.ShowWarning("ScreenText could not stop a capture cleanly. The application will now exit.");
        }
        finally
        {
            System.Windows.Application.Current.Shutdown();
        }
    }

    public void Dispose()
    {
        _settingsWindow?.Close();
        if (_activationPollTimer is not null)
        {
            _activationPollTimer.Stop();
            _activationPollTimer.Tick -= ActivationPollTimerOnTick;
        }
        _activationPollTimer = null;
        _activationEvent?.Dispose();
        _activationEvent = null;
        _hotkey?.Dispose();
        _captureCoordinator?.Dispose();
        _ocrService?.Dispose();
        _tray?.Dispose();
        if (_ownsMutex)
        {
            _singleInstanceMutex.ReleaseMutex();
        }
        _singleInstanceMutex.Dispose();
    }
}
