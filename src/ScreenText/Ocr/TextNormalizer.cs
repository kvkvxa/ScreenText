namespace ScreenText.Ocr;

public static class TextNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var withoutNul = value.Replace("\0", string.Empty, StringComparison.Ordinal);
        var lines = withoutNul.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = new string(lines[i].Where(character => !char.IsControl(character) || character == '\t').ToArray()).TrimEnd(' ', '\t');
        }

        return string.Join("\r\n", lines).Trim();
    }
}
