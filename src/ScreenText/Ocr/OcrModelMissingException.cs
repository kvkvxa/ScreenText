namespace ScreenText.Ocr;

public sealed class OcrModelMissingException : Exception
{
    public OcrModelMissingException(string message, string? modelCode = null) : base(message)
    {
        ModelCode = modelCode;
    }

    public string? ModelCode { get; }
}
