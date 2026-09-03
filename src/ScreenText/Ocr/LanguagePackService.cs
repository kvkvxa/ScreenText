using System.IO;
using System.Security.Cryptography;

namespace ScreenText.Ocr;

public sealed record LanguagePackImportResult(string Code, string FileName);

public sealed class LanguagePackService
{
    private readonly string _modelsPath;

    public LanguagePackService(string? modelsPath = null)
    {
        if (modelsPath is not null)
        {
            _modelsPath = modelsPath;
            return;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var currentPath = Path.Combine(localAppData, "ScreenText", "models");
        var legacyPath = Path.Combine(localAppData, "OCRTool", "models");
        _modelsPath = Directory.Exists(currentPath) || !Directory.Exists(legacyPath) ? currentPath : legacyPath;
    }

    public string ModelsPath => _modelsPath;

    public LanguagePackImportResult Import(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException("A model file is required.", nameof(sourcePath));
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("The selected model file was not found.", sourcePath);
        if (!string.Equals(Path.GetExtension(sourcePath), ".traineddata", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Only Tesseract .traineddata files can be imported.");

        var fileName = Path.GetFileName(sourcePath);
        var code = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
        if (!OcrLanguageParser.TryParse(code, out _))
            throw new InvalidDataException("The model filename is not a valid Tesseract language code.");

        Directory.CreateDirectory(_modelsPath);
        var destinationPath = Path.Combine(_modelsPath, code + ".traineddata");
        var temporaryPath = destinationPath + ".tmp";
        try
        {
            File.Copy(sourcePath, temporaryPath, true);
            string hash;
            using (var stream = File.OpenRead(temporaryPath))
            using (var sha256 = SHA256.Create())
                hash = Convert.ToHexString(sha256.ComputeHash(stream));
            File.Move(temporaryPath, destinationPath, true);
            WriteManifestEntry(code + ".traineddata", hash);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }

        return new LanguagePackImportResult(code, fileName);
    }

    private void WriteManifestEntry(string fileName, string hash)
    {
        var manifestPath = Path.Combine(_modelsPath, "SHA256SUMS.txt");
        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var commentLines = new List<string>();
        if (File.Exists(manifestPath))
        {
            foreach (var line in File.ReadLines(manifestPath))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith('#'))
                {
                    commentLines.Add(line);
                    continue;
                }
                var parts = trimmed.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && parts[0].Length == 64) entries[parts[1].Trim()] = parts[0];
            }
        }

        entries[fileName] = hash;
        var temporaryManifest = manifestPath + ".tmp";
        try
        {
            var lines = new List<string>(commentLines.Count + entries.Count);
            lines.AddRange(commentLines);
            if (commentLines.Count > 0 && entries.Count > 0) lines.Add(""); // Blank line separates comments from checksum entries.
            lines.AddRange(entries.OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => $"{entry.Value}  {entry.Key}"));
            File.WriteAllLines(temporaryManifest, lines);
            using (var stream = new FileStream(temporaryManifest, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                stream.Flush(true);
            File.Move(temporaryManifest, manifestPath, true);
        }
        finally
        {
            if (File.Exists(temporaryManifest)) File.Delete(temporaryManifest);
        }
    }
}
