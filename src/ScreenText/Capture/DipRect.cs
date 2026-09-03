namespace ScreenText.Capture;

public readonly record struct DipRect(double Left, double Top, double Right, double Bottom)
{
    public static DipRect FromPhysical(PhysicalRect rect, double dpiScale) => new(
        rect.Left / dpiScale, rect.Top / dpiScale, rect.Right / dpiScale, rect.Bottom / dpiScale);
}
