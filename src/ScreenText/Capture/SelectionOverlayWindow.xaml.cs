using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shapes;
using ScreenText.Platform;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace ScreenText.Capture;

public partial class SelectionOverlayWindow : Window
{
    private readonly MonitorInfo _monitor;
    private readonly Action<SelectionOverlayWindow, PhysicalPoint> _mouseDown;
    private readonly Action<PhysicalPoint> _mouseMove;
    private readonly Action<SelectionOverlayWindow> _mouseUp;
    private readonly Action _cancel;
    private double _dpiScaleX = 1;
    private double _dpiScaleY = 1;

    public SelectionOverlayWindow(
        MonitorInfo monitor,
        Action<SelectionOverlayWindow, PhysicalPoint> mouseDown,
        Action<PhysicalPoint> mouseMove,
        Action<SelectionOverlayWindow> mouseUp,
        Action cancel)
    {
        InitializeComponent();
        _monitor = monitor;
        _mouseDown = mouseDown;
        _mouseMove = mouseMove;
        _mouseUp = mouseUp;
        _cancel = cancel;
        SourceInitialized += OnSourceInitialized;
        PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
        PreviewMouseMove += OnMouseMove;
        PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var dpi = NativeMethods.GetDpiForWindow(hwnd);
        _dpiScaleX = dpi == 0 ? _monitor.DpiScaleX : dpi / 96d;
        _dpiScaleY = dpi == 0 ? _monitor.DpiScaleY : dpi / 96d;
        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, _monitor.Bounds.Left, _monitor.Bounds.Top,
            _monitor.Bounds.Width, _monitor.Bounds.Height, NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        CaptureMouse();
        InstructionBorder.Visibility = Visibility.Collapsed;
        _mouseDown(this, GetCursorPhysicalPoint());
        e.Handled = true;
    }

    private void OnMouseMove(object sender, WpfMouseEventArgs e)
    {
        if (IsMouseCaptured) _mouseMove(GetCursorPhysicalPoint());
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsMouseCaptured) ReleaseMouseCapture();
        _mouseUp(this);
        e.Handled = true;
    }

    private void OnPreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _cancel();
            e.Handled = true;
        }
    }

    private PhysicalPoint GetCursorPhysicalPoint()
    {
        if (!NativeMethods.GetCursorPos(out var point)) throw new InvalidOperationException("Unable to read cursor position.");
        return new PhysicalPoint(point.X, point.Y);
    }

    public void RenderSelection(PhysicalRect? globalSelection)
    {
        var clipped = globalSelection?.Intersect(_monitor.Bounds);
        if (clipped is null || clipped.Value.IsEmpty)
        {
            SelectionRectangle.Visibility = Visibility.Collapsed;
            return;
        }

        var rect = clipped.Value;
        Canvas.SetLeft(SelectionRectangle, (rect.Left - _monitor.Bounds.Left) / _dpiScaleX);
        Canvas.SetTop(SelectionRectangle, (rect.Top - _monitor.Bounds.Top) / _dpiScaleY);
        SelectionRectangle.Width = rect.Width / _dpiScaleX;
        SelectionRectangle.Height = rect.Height / _dpiScaleY;
        SelectionRectangle.Visibility = Visibility.Visible;
    }
}
