namespace ScreenText.Capture;

public sealed class ScreenCaptureException : Exception
{
    public ScreenCaptureException(string message) : base(message) { }
    public ScreenCaptureException(string message, Exception innerException) : base(message, innerException) { }
}
