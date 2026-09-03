using System;
using System.IO;
using System.Text.Json;
using ScreenText.Platform;
using ScreenText.Settings;
using Xunit;

namespace ScreenText.Tests;

public sealed class SettingsTests
{
    [Fact]
    public void DefaultsMatchApprovedConfiguration()
    {
        var settings = new AppSettings();
        Assert.Equal(1, settings.SchemaVersion);
        Assert.Equal("eng+rus", settings.Language);
        Assert.True(settings.AutoCopy);
        Assert.True(settings.ShowNotification);
        Assert.False(settings.StartWithWindows);
        Assert.Equal(["Win", "Shift"], settings.Hotkey.Modifiers);
        Assert.Equal("O", settings.Hotkey.Key);
    }

    [Fact]
    public void SettingsRoundTripAsJson()
    {
        var settings = new AppSettings { StartWithWindows = true };
        var json = JsonSerializer.Serialize(settings);
        var restored = JsonSerializer.Deserialize<AppSettings>(json);
        Assert.NotNull(restored);
        Assert.True(restored!.StartWithWindows);
    }

    [Fact]
    public void SettingsServiceUsesApprovedCamelCaseSchema()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ScreenTextTests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "settings.json");

        try
        {
            var service = new SettingsService(path);
            service.Save(new AppSettings { StartWithWindows = true });
            Assert.Contains("\"schemaVersion\"", File.ReadAllText(path));

            File.WriteAllText(path, "{\"schemaVersion\":1,\"hotkey\":{\"modifiers\":[\"Ctrl\"],\"key\":\"F2\"},\"language\":\"rus\",\"autoCopy\":false,\"showNotification\":false,\"startWithWindows\":true}");
            var restored = service.Load();

            Assert.Equal(["Ctrl"], restored.Hotkey.Modifiers);
            Assert.Equal("F2", restored.Hotkey.Key);
            Assert.Equal("rus", restored.Language);
            Assert.False(restored.AutoCopy);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void SettingsPreserveAnArbitrarySafeLanguageSelection()
    {
        var settings = new AppSettings { Language = "spa+chi_sim" };

        settings.Normalize();

        Assert.Equal("spa+chi_sim", settings.Language);
    }

    [Fact]
    public void NormalizeRepairsPartiallyCorruptSettings()
    {
        var settings = new AppSettings
        {
            SchemaVersion = 99,
            Language = null!,
            Hotkey = null!
        };

        settings.Normalize();

        Assert.Equal(1, settings.SchemaVersion);
        Assert.Equal("eng+rus", settings.Language);
        Assert.NotNull(settings.Hotkey);
        Assert.Equal(["Win", "Shift"], settings.Hotkey.Modifiers);
        Assert.Equal("O", settings.Hotkey.Key);
    }

    [Fact]
    public void HotkeyParserAcceptsConfiguredWinShiftShortcut()
    {
        var settings = new HotkeySettings { Modifiers = ["Win", "Shift"], Key = "O" };

        var parsed = HotkeyService.TryParse(settings, out var modifiers, out var virtualKey);

        Assert.True(parsed);
        Assert.Equal(NativeMethods.MOD_WIN | NativeMethods.MOD_SHIFT, modifiers);
        Assert.NotEqual(0u, virtualKey);
    }

    [Fact]
    public void HotkeyParserRejectsShortcutWithoutModifier()
    {
        var settings = new HotkeySettings { Modifiers = [], Key = "O" };

        Assert.False(HotkeyService.TryParse(settings, out _, out _));
    }
}
