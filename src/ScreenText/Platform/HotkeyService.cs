using System.Windows.Interop;
using System.Windows.Input;
using ScreenText.Settings;

namespace ScreenText.Platform;

public sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 0x4F52;
    private const int AlternateHotkeyId = HotkeyId + 1;
    private HwndSource? _source;
    private Action? _pressed;
    private bool _registered;
    private int _registeredId;
    private uint _registeredModifiers;
    private uint _registeredVirtualKey;

    public bool Register(HotkeySettings settings, Action pressed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(pressed);
        _pressed = pressed;
        EnsureSource();

        if (!TryParse(settings, out var modifiers, out var virtualKey)) return false;
        if (_registered && _registeredModifiers == modifiers && _registeredVirtualKey == virtualKey) return true;
        var candidateId = !_registered || _registeredId == AlternateHotkeyId ? HotkeyId : AlternateHotkeyId;
        if (!NativeMethods.RegisterHotKey(_source!.Handle, candidateId, modifiers, virtualKey)) return false;

        if (_registered) NativeMethods.UnregisterHotKey(_source.Handle, _registeredId);
        _registeredId = candidateId;
        _registeredModifiers = modifiers;
        _registeredVirtualKey = virtualKey;
        _registered = true;
        return true;
    }

    private void EnsureSource()
    {
        if (_source is not null) return;

        var parameters = new HwndSourceParameters("ScreenTextHotkey")
        {
            Width = 0,
            Height = 0,
            WindowStyle = unchecked((int)0x80000000)
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WindowProc);
    }

    public static bool TryParse(HotkeySettings settings, out uint modifiers, out uint virtualKey)
    {
        modifiers = 0;
        virtualKey = 0;
        foreach (var modifier in settings.Modifiers ?? [])
        {
            modifiers |= modifier.ToUpperInvariant() switch
            {
                "WIN" => NativeMethods.MOD_WIN,
                "SHIFT" => NativeMethods.MOD_SHIFT,
                "CTRL" => NativeMethods.MOD_CONTROL,
                "ALT" => NativeMethods.MOD_ALT,
                _ => 0
            };
        }

        if (modifiers == 0 || !Enum.TryParse<Key>(settings.Key ?? string.Empty, true, out var key) || key == Key.None) return false;
        virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
        return virtualKey != 0;
    }

    private IntPtr WindowProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == NativeMethods.WM_HOTKEY && _registered && wParam.ToInt32() == _registeredId)
        {
            _pressed?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_source is not null)
        {
            if (_registered) NativeMethods.UnregisterHotKey(_source.Handle, _registeredId);
            _source.RemoveHook(WindowProc);
            _source.Dispose();
        }
        _source = null;
        _pressed = null;
        _registered = false;
        _registeredId = 0;
        _registeredModifiers = 0;
        _registeredVirtualKey = 0;
    }
}
