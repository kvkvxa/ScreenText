using ScreenText.Ocr;

namespace ScreenText.Settings;

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public HotkeySettings Hotkey { get; set; } = new();
    public string Language { get; set; } = "eng+rus";
    public bool AutoCopy { get; set; } = true;
    public bool ShowNotification { get; set; } = true;
    public bool StartWithWindows { get; set; }

    public void Normalize()
    {
        if (SchemaVersion != 1) SchemaVersion = 1;
        Hotkey ??= new HotkeySettings();
        Hotkey.Normalize();
        Language = OcrLanguageParser.Parse(Language).Code;
    }
}

public sealed class HotkeySettings
{
    public List<string> Modifiers { get; set; } = ["Win", "Shift"];
    public string Key { get; set; } = "O";

    public HotkeySettings Clone() => new()
    {
        Modifiers = Modifiers is null ? [] : [.. Modifiers],
        Key = Key
    };

    public void Normalize()
    {
        Modifiers ??= ["Win", "Shift"];
        Modifiers = Modifiers
            .Where(modifier => modifier is "Win" or "Shift" or "Ctrl" or "Alt")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (Modifiers.Count == 0) Modifiers = ["Win", "Shift"];
        Key = string.IsNullOrWhiteSpace(Key) ? "O" : Key.Trim().ToUpperInvariant();
    }
}
