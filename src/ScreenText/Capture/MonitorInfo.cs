namespace ScreenText.Capture;

public sealed record MonitorInfo(nint Handle, PhysicalRect Bounds, PhysicalRect WorkArea, uint DpiX, uint DpiY)
{
    public double DpiScaleX => DpiX / 96d;
    public double DpiScaleY => DpiY / 96d;
}
