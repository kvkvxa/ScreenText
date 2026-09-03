using System.IO;
using System.Security.Cryptography;

namespace ScreenText.Ocr;

public sealed class OcrModelCatalog
{
    private readonly string _bundledDataPath;
    private readonly string? _customDataPath;

    public OcrModelCatalog(string bundledDataPath, string? customDataPath = null)
    {
        _bundledDataPath = bundledDataPath;
        _customDataPath = string.IsNullOrWhiteSpace(customDataPath) ? null : customDataPath;
    }

    public IReadOnlyList<string> AvailableLanguageCodes
    {
        get
        {
            var codes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var path in GetModelDirectories())
            {
                if (!Directory.Exists(path)) continue;
                foreach (var file in Directory.EnumerateFiles(path, "*.traineddata", SearchOption.TopDirectoryOnly))
                {
                    var code = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                    if (!code.Equals("osd", StringComparison.Ordinal) && OcrLanguageParser.TryParse(code, out _))
                        codes.Add(code);
                }
            }

            return codes.OrderBy(code => code, StringComparer.Ordinal).ToArray();
        }
    }

    public bool HasModel(string code) => TryGetModelPath(code, out _);

    public bool HasOsdModel => HasModel("osd");

    public string GetModelPath(string code)
    {
        if (!TryGetModelPath(code, out var path))
            throw new OcrModelMissingException($"Tesseract model is missing: {code}.", code);
        return path;
    }

    public string GetSelectionSignature(IEnumerable<string> languageCodes)
    {
        return string.Join('|', languageCodes.Select(code =>
        {
            var path = GetModelPath(code);
            var info = new FileInfo(path);
            return $"{path}:{info.Length}:{info.LastWriteTimeUtc.Ticks}";
        }));
    }

    public void VerifyIntegrity(IEnumerable<string> languageCodes)
    {
        var codes = languageCodes.Distinct(StringComparer.Ordinal).ToArray();
        if (codes.Length == 0) throw new OcrModelIntegrityException("No OCR model was selected.");

        foreach (var source in codes.GroupBy(GetManifestPath))
            VerifyManifest(source.Key, source);
    }

    private bool TryGetModelPath(string code, out string path)
    {
        var safeCode = OcrLanguageParser.NormalizeOrThrow(code);
        var fileName = safeCode + ".traineddata";
        if (_customDataPath is not null)
        {
            path = Path.Combine(_customDataPath, fileName);
            if (File.Exists(path)) return true;
        }

        path = Path.Combine(_bundledDataPath, fileName);
        return File.Exists(path);
    }

    private string GetManifestPath(string code)
    {
        var modelPath = GetModelPath(code);
        var directory = Path.GetDirectoryName(modelPath) ?? throw new InvalidOperationException("OCR model directory is unavailable.");
        return Path.Combine(directory, "SHA256SUMS.txt");
    }

    private void VerifyManifest(string manifestPath, IEnumerable<string> languageCodes)
    {
        if (!File.Exists(manifestPath))
            throw new OcrModelIntegrityException("OCR model checksum manifest is missing.");

        var expectedHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var rawLine in File.ReadLines(manifestPath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#')) continue;
                var parts = line.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2 || parts[0].Length != 64)
                    throw new OcrModelIntegrityException("OCR model checksum manifest is invalid.");
                expectedHashes[parts[1].Trim()] = parts[0];
            }

            foreach (var code in languageCodes)
            {
                var fileName = code + ".traineddata";
                if (!expectedHashes.TryGetValue(fileName, out var expectedHash))
                    throw new OcrModelIntegrityException($"OCR model checksum is missing: {fileName}.");

                using var stream = File.OpenRead(GetModelPath(code));
                using var sha256 = SHA256.Create();
                var actualHash = Convert.ToHexString(sha256.ComputeHash(stream));
                if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                    throw new OcrModelIntegrityException($"OCR model checksum mismatch: {fileName}.");
            }
        }
        catch (OcrModelIntegrityException)
        {
            throw;
        }
        catch (IOException exception)
        {
            throw new OcrModelIntegrityException("OCR model integrity could not be verified.", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new OcrModelIntegrityException("OCR model integrity could not be verified.", exception);
        }
    }

    private IEnumerable<string> GetModelDirectories()
    {
        if (_customDataPath is not null) yield return _customDataPath;
        yield return _bundledDataPath;
    }
}
