namespace ScreenText.Capture;

public readonly record struct DipPoint(double X, double Y)
{
    public PhysicalPoint ToPhysical(double dpiScale) => new((int)Math.Round(X * dpiScale), (int)Math.Round(Y * dpiScale));
}
