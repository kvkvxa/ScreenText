namespace ScreenText.Ocr;

public static class OcrScriptRouter
{
    private static readonly IReadOnlyDictionary<string, string[]> ScriptModels =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Latin"] = ["eng", "spa", "fra", "deu", "ita", "por", "nld", "pol", "tur"],
            ["Cyrillic"] = ["rus", "ukr", "bul", "srp", "mkd"],
            ["Han"] = ["chi_sim", "chi_tra"],
            ["Hiragana"] = ["jpn"],
            ["Katakana"] = ["jpn"],
            ["Japanese"] = ["jpn"],
            ["Hangul"] = ["kor"],
            ["Arabic"] = ["ara", "fas", "urd"],
            ["Hebrew"] = ["heb"],
            ["Greek"] = ["ell"],
            ["Devanagari"] = ["hin", "mar", "nep"],
            ["Bengali"] = ["ben"],
            ["Gujarati"] = ["guj"],
            ["Gurmukhi"] = ["pan"],
            ["Tamil"] = ["tam"],
            ["Telugu"] = ["tel"],
            ["Kannada"] = ["kan"],
            ["Malayalam"] = ["mal"],
            ["Thai"] = ["tha"],
            ["Lao"] = ["lao"],
            ["Myanmar"] = ["mya"],
            ["Khmer"] = ["khm"],
            ["Sinhala"] = ["sin"],
            ["Tibetan"] = ["bod"],
            ["Georgian"] = ["kat"],
            ["Armenian"] = ["hye"],
            ["Ethiopic"] = ["amh"],
        };

    public static IReadOnlyList<string> ResolveModels(IEnumerable<string> scripts, IEnumerable<string> availableModels)
    {
        var available = new HashSet<string>(availableModels, StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var script in scripts.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!ScriptModels.TryGetValue(script, out var candidates))
                throw new OcrUnsupportedScriptException("OCR detected a script without a matching local language model.");

            foreach (var candidate in candidates)
            {
                if (available.Contains(candidate) && !result.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                    result.Add(candidate);
            }

            if (!result.Any(candidate => candidates.Contains(candidate, StringComparer.OrdinalIgnoreCase)))
                throw new OcrUnsupportedScriptException("OCR detected a script without a matching local language model.");
        }

        return result;
    }
}
