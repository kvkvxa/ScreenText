using System.Collections.ObjectModel;

namespace ScreenText.Ocr;

// Kept for source compatibility with the first ScreenText API. New code uses OcrLanguageSelection.
public enum OcrLanguage
{
    English,
    Russian,
    EnglishAndRussian
}

public readonly record struct OcrLanguageSelection
{
    public const string AutoCode = "auto";

    public OcrLanguageSelection(string code)
    {
        Code = OcrLanguageParser.NormalizeOrThrow(code);
    }

    public string Code { get; } = string.Empty;

    public bool IsAuto => Code.Equals(AutoCode, StringComparison.Ordinal);

    public IReadOnlyList<string> Codes => IsAuto
        ? Array.Empty<string>()
        : new ReadOnlyCollection<string>(Code.Split('+', StringSplitOptions.RemoveEmptyEntries));
}

public static class OcrLanguageParser
{
    public static OcrLanguageSelection Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new("eng+rus");
        return TryNormalize(value, out var normalized) ? new(normalized) : new("eng+rus");
    }

    public static bool TryParse(string? value, out OcrLanguageSelection selection)
    {
        if (TryNormalize(value, out var normalized))
        {
            selection = new OcrLanguageSelection(normalized);
            return true;
        }

        selection = default;
        return false;
    }

    public static string ToTesseractCode(OcrLanguageSelection selection)
    {
        if (selection.IsAuto) throw new ArgumentException("Auto does not have a Tesseract language code.", nameof(selection));
        return selection.Code;
    }

    public static string ToTesseractCode(OcrLanguage language) => language switch
    {
        OcrLanguage.English => "eng",
        OcrLanguage.Russian => "rus",
        OcrLanguage.EnglishAndRussian => "eng+rus",
        _ => throw new ArgumentOutOfRangeException(nameof(language))
    };

    public static OcrLanguageSelection FromLegacy(OcrLanguage language) => new(ToTesseractCode(language));

    internal static string NormalizeOrThrow(string value)
    {
        if (!TryNormalize(value, out var normalized))
            throw new ArgumentException("The OCR language selection is invalid.", nameof(value));
        return normalized;
    }

    private static bool TryNormalize(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var parts = value.Trim().ToLowerInvariant().Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;
        if (parts.Length == 1 && parts[0] == OcrLanguageSelection.AutoCode)
        {
            normalized = OcrLanguageSelection.AutoCode;
            return true;
        }

        if (parts.Any(part => part == OcrLanguageSelection.AutoCode || !IsSafeModelCode(part))) return false;
        normalized = string.Join('+', parts.Distinct(StringComparer.Ordinal));
        return normalized.Length > 0;
    }

    private static bool IsSafeModelCode(string value)
    {
        if (value.Length is < 2 or > 32) return false;
        return value.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_');
    }
}
