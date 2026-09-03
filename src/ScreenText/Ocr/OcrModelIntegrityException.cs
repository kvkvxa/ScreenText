namespace ScreenText.Ocr;

public sealed class OcrModelIntegrityException : Exception
{
    public OcrModelIntegrityException(string message) : base(message) { }

    public OcrModelIntegrityException(string message, Exception innerException) : base(message, innerException) { }
}
