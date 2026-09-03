namespace ScreenText.Platform;

public sealed class NotificationService
{
    private readonly TrayService _tray;

    public NotificationService(TrayService tray) => _tray = tray;

    public void ShowInfo(string message) => _tray.ShowInfo(message);

    public void ShowWarning(string message) => _tray.ShowWarning(message);
}
