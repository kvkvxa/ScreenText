namespace ScreenText.Ocr;

public sealed record OcrResult(
    string Text,
    float MeanConfidence,
    string? DetectedScript = null,
    IReadOnlyList<string>? UsedLanguageCodes = null);
