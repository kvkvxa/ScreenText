namespace ScreenText.Platform;

public sealed class ClipboardUnavailableException : Exception
{
    public ClipboardUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}
