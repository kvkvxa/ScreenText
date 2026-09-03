using System.Drawing;
using System.Windows.Forms;

namespace ScreenText.Platform;

public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Action _onCapture;
    private readonly Action _onCaptureImage;
    private readonly Action _onSettings;
    private readonly Func<Task> _onExit;

    public TrayService(Action onCapture, Action onCaptureImage, Action onSettings, Func<Task> onExit)
    {
        _onCapture = onCapture;
        _onCaptureImage = onCaptureImage;
        _onSettings = onSettings;
        _onExit = onExit;
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "ScreenText",
            Visible = false,
            ContextMenuStrip = CreateMenu()
        };
        _notifyIcon.DoubleClick += (_, _) => _onCapture();
    }

    public void Show() => _notifyIcon.Visible = true;

    public void ShowWarning(string message)
    {
        _notifyIcon.BalloonTipTitle = "ScreenText";
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(5000);
    }

    public void ShowInfo(string message)
    {
        _notifyIcon.BalloonTipTitle = "ScreenText";
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(3000);
    }

    private ContextMenuStrip CreateMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Capture text", null, (_, _) => _onCapture());
        menu.Items.Add("Capture screenshot", null, (_, _) => _onCaptureImage());
        menu.Items.Add("Settings", null, (_, _) => _onSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, async (_, _) => await _onExit());
        return menu;
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
