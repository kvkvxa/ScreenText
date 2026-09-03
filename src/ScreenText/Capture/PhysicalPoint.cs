namespace ScreenText.Capture;

public readonly record struct PhysicalPoint(int X, int Y)
{
    public static PhysicalPoint Min(PhysicalPoint a, PhysicalPoint b) => new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
    public static PhysicalPoint Max(PhysicalPoint a, PhysicalPoint b) => new(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
}
