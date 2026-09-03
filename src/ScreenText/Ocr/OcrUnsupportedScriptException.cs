namespace ScreenText.Ocr;

public sealed class OcrUnsupportedScriptException : Exception
{
    public OcrUnsupportedScriptException(string message) : base(message) { }
}
