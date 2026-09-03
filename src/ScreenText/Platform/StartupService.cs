using Microsoft.Win32;

namespace ScreenText.Platform;

public sealed class StartupService
{
    private const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string ValueName = "ScreenText";
    private const string LegacyValueName = "OCRTool";

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey)
            ?? throw new InvalidOperationException("Unable to open the Windows startup registry key.");
        if (enabled)
        {
            var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Process path is unavailable.");
            key.SetValue(ValueName, $"\"{executable}\"");
            key.DeleteValue(LegacyValueName, false);
        }
        else
        {
            key.DeleteValue(ValueName, false);
            key.DeleteValue(LegacyValueName, false);
        }
    }

}
