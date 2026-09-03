namespace ScreenText.Capture;

public readonly record struct PhysicalRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;
    public int Height => Bottom - Top;
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public static PhysicalRect FromPoints(PhysicalPoint a, PhysicalPoint b) => new(
        Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

    public PhysicalRect Intersect(PhysicalRect other) => new(
        Math.Max(Left, other.Left), Math.Max(Top, other.Top), Math.Min(Right, other.Right), Math.Min(Bottom, other.Bottom));

    public bool Contains(PhysicalPoint point) => point.X >= Left && point.X < Right && point.Y >= Top && point.Y < Bottom;
}
