using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Security;
using Microsoft.Win32;
using ScreenText.Ocr;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace ScreenText.Settings;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly SettingsService _service;
    private readonly LanguagePackService _languagePacks;
    private readonly Func<HotkeySettings, bool> _applyHotkey;

    public SettingsWindow(AppSettings settings, SettingsService service, LanguagePackService languagePacks, Func<HotkeySettings, bool> applyHotkey)
    {
        InitializeComponent();
        _settings = settings;
        _service = service;
        _languagePacks = languagePacks;
        _applyHotkey = applyHotkey;
        WinModifierCheckBox.IsChecked = HasModifier("Win");
        ShiftModifierCheckBox.IsChecked = HasModifier("Shift");
        CtrlModifierCheckBox.IsChecked = HasModifier("Ctrl");
        AltModifierCheckBox.IsChecked = HasModifier("Alt");
        HotkeyKeyTextBox.Text = settings.Hotkey.Key;
        UpdateHotkeyPreview();
        LanguageTextBox.Text = settings.Language;
        StartWithWindowsCheckBox.IsChecked = settings.StartWithWindows;
        AutoCopyCheckBox.IsChecked = settings.AutoCopy;
        ShowNotificationCheckBox.IsChecked = settings.ShowNotification;
    }

    private void SaveClick(object sender, RoutedEventArgs e)
    {
        var previousHotkey = _settings.Hotkey.Clone();
        var newHotkey = new HotkeySettings
        {
            Modifiers = GetSelectedModifiers(),
            Key = HotkeyKeyTextBox.Text
        };
        newHotkey.Normalize();
        if (!_applyHotkey(newHotkey))
        {
            StatusText.Text = "The selected global hotkey is unavailable or invalid.";
            return;
        }

        _settings.Hotkey = newHotkey;
        _settings.StartWithWindows = StartWithWindowsCheckBox.IsChecked == true;
        _settings.Language = LanguageTextBox.Text;
        _settings.AutoCopy = AutoCopyCheckBox.IsChecked == true;
        _settings.ShowNotification = ShowNotificationCheckBox.IsChecked == true;
        _settings.Normalize();
        try
        {
            _service.Save(_settings);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException or InvalidOperationException)
        {
            _applyHotkey(previousHotkey);
            _settings.Hotkey = previousHotkey;
            StatusText.Text = "Could not save settings. Check file and registry access, then try again.";
            return;
        }

        try
        {
            _service.ApplyStartupSetting(_settings.StartWithWindows);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or SecurityException or InvalidOperationException)
        {
            StatusText.Text = "Settings were saved, but the Windows startup option could not be updated.";
            return;
        }

        Close();
    }

    private void CancelClick(object sender, RoutedEventArgs e) => Close();

    private void AddLanguageModelClick(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfOpenFileDialog
        {
            Title = "Add Tesseract language model",
            Filter = "Tesseract language model (*.traineddata)|*.traineddata",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var imported = _languagePacks.Import(dialog.FileName);
            if (!imported.Code.Equals("osd", StringComparison.OrdinalIgnoreCase))
            {
                var selected = OcrLanguageParser.Parse(LanguageTextBox.Text);
                if (!selected.IsAuto && !selected.Codes.Contains(imported.Code, StringComparer.OrdinalIgnoreCase))
                    LanguageTextBox.Text = string.Join('+', selected.Codes.Append(imported.Code));
            }
            StatusText.Foreground = System.Windows.Media.Brushes.SeaGreen;
            StatusText.Text = $"Added {imported.Code}. Save changes to use it.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or IOException or UnauthorizedAccessException or SecurityException)
        {
            StatusText.Foreground = System.Windows.Media.Brushes.Firebrick;
            StatusText.Text = "The model could not be added. Choose a valid .traineddata file and check folder access.";
        }
    }

    private void HotkeyKeyTextBoxPreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin)
        {
            return;
        }

        if (key == Key.None) return;
        HotkeyKeyTextBox.Text = key.ToString().ToUpperInvariant();
        HotkeyKeyTextBox.CaretIndex = HotkeyKeyTextBox.Text.Length;
        WinModifierCheckBox.IsChecked = Keyboard.Modifiers.HasFlag(ModifierKeys.Windows);
        ShiftModifierCheckBox.IsChecked = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        CtrlModifierCheckBox.IsChecked = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        AltModifierCheckBox.IsChecked = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
        UpdateHotkeyPreview();
        e.Handled = true;
    }

    private void ModifierChanged(object sender, RoutedEventArgs e) => UpdateHotkeyPreview();

    private void HotkeyKeyTextBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => UpdateHotkeyPreview();

    private void UpdateHotkeyPreview()
    {
        var parts = GetSelectedModifiers();
        if (!string.IsNullOrWhiteSpace(HotkeyKeyTextBox.Text)) parts.Add(HotkeyKeyTextBox.Text.Trim().ToUpperInvariant());
        HotkeyPreviewText.Text = string.Join(" + ", parts);
    }

    private bool HasModifier(string modifier) => _settings.Hotkey.Modifiers.Contains(modifier, StringComparer.OrdinalIgnoreCase);

    private List<string> GetSelectedModifiers()
    {
        var modifiers = new List<string>();
        if (WinModifierCheckBox.IsChecked == true) modifiers.Add("Win");
        if (ShiftModifierCheckBox.IsChecked == true) modifiers.Add("Shift");
        if (CtrlModifierCheckBox.IsChecked == true) modifiers.Add("Ctrl");
        if (AltModifierCheckBox.IsChecked == true) modifiers.Add("Alt");
        return modifiers;
    }
}
