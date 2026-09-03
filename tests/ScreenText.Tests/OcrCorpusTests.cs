using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ScreenText.Ocr;
using Xunit;

namespace ScreenText.Tests;

public sealed class OcrCorpusTests
{
    [Fact]
    public async Task RealScreenshotCorpusContainsExpectedPhrases()
    {
        var corpusDirectory = Path.Combine(AppContext.BaseDirectory, "test-data", "ocr");
        var images = Directory.Exists(corpusDirectory)
            ? Directory.GetFiles(corpusDirectory, "*.png", SearchOption.TopDirectoryOnly).OrderBy(path => path).ToArray()
            : [];
        if (images.Length == 0)
        {
            Console.WriteLine("Real OCR corpus is not present; corpus assertions are dormant.");
            return;
        }

        using var service = new OcrService();
        foreach (var imagePath in images)
        {
            var expectedPath = Path.ChangeExtension(imagePath, ".txt");
            Assert.True(File.Exists(expectedPath), $"Missing expected phrases file: {expectedPath}");
            var expectedPhrases = File.ReadAllLines(expectedPath)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToArray();
            Assert.NotEmpty(expectedPhrases);

            using var bitmap = new Bitmap(imagePath);
            var result = await service.RecognizeAsync(bitmap, OcrLanguage.EnglishAndRussian);
            foreach (var phrase in expectedPhrases)
            {
                Assert.True(result.Text.Contains(phrase, StringComparison.OrdinalIgnoreCase),
                    $"OCR result for {Path.GetFileName(imagePath)} did not contain expected phrase '{phrase}'.");
            }
        }
    }
}
