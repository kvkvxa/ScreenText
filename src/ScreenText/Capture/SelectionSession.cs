namespace ScreenText.Capture;

public sealed class SelectionSession
{
    public PhysicalPoint Start { get; }
    public PhysicalRect CurrentRect { get; private set; }

    public SelectionSession(PhysicalPoint start)
    {
        Start = start;
        CurrentRect = new PhysicalRect(start.X, start.Y, start.X, start.Y);
    }

    public void Update(PhysicalPoint current) => CurrentRect = PhysicalRect.FromPoints(Start, current);
}
