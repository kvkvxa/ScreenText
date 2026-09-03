using System.Drawing;
using System.Threading.Tasks;
using ScreenText.Ocr;
using Xunit;

namespace ScreenText.Tests;

public sealed class OcrServiceTests
{
    [Fact]
    public async Task InitializesBundledEngineAndReturnsResult()
    {
        using var bitmap = new Bitmap(160, 80);
        using var service = new OcrService();

        var result = await service.RecognizeAsync(bitmap, OcrLanguage.EnglishAndRussian);

        Assert.NotNull(result);
        Assert.NotNull(result.Text);
    }

    [Fact]
    public async Task ReusesAndSwitchesEngineByLanguage()
    {
        using var bitmap = new Bitmap(160, 80);
        using var service = new OcrService();

        await service.RecognizeAsync(bitmap, OcrLanguage.English);
        var result = await service.RecognizeAsync(bitmap, OcrLanguage.Russian);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task AutoModeFailsClosedWhenScriptModelIsNotInstalled()
    {
        using var bitmap = new Bitmap(160, 80);
        using var service = new OcrService();

        await Assert.ThrowsAsync<OcrModelMissingException>(() =>
            service.RecognizeAsync(bitmap, OcrLanguageParser.Parse("auto")));
    }

    [Fact]
    public async Task ExplicitUnbundledLanguageFailsInsteadOfFallingBack()
    {
        using var bitmap = new Bitmap(160, 80);
        using var service = new OcrService();

        await Assert.ThrowsAsync<OcrModelMissingException>(() =>
            service.RecognizeAsync(bitmap, OcrLanguageParser.Parse("spa")));
    }

    [Fact]
    public async Task RecognizesLargeEnglishText()
    {
        using var bitmap = new Bitmap(420, 100);
        using (var graphics = Graphics.FromImage(bitmap))
        using (var font = new Font("Arial", 28, FontStyle.Bold))
        {
            graphics.Clear(Color.White);
            graphics.DrawString("OCR TEST", font, Brushes.Black, new PointF(8, 20));
        }

        using var service = new OcrService();
        var result = await service.RecognizeAsync(bitmap, OcrLanguage.English);

        Assert.Contains("OCR", result.Text.ToUpperInvariant());
    }

    [Fact]
    public async Task RecognizesEnglishTextOnDarkBackground()
    {
        using var bitmap = new Bitmap(420, 100);
        using (var graphics = Graphics.FromImage(bitmap))
        using (var font = new Font("Arial", 28, FontStyle.Bold))
        {
            graphics.Clear(Color.FromArgb(24, 24, 24));
            graphics.DrawString("DARK THEME", font, Brushes.White, new PointF(8, 20));
        }

        using var service = new OcrService();
        var result = await service.RecognizeAsync(bitmap, OcrLanguage.English);

        Assert.Contains("DARK", result.Text.ToUpperInvariant());
    }

    [Fact]
    public async Task RecognizesMixedEnglishAndRussianText()
    {
        using var bitmap = new Bitmap(720, 130);
        using (var graphics = Graphics.FromImage(bitmap))
        using (var font = new Font("Arial", 28, FontStyle.Bold))
        {
            graphics.Clear(Color.White);
            graphics.DrawString("OCR TEST Привет МИР", font, Brushes.Black, new PointF(8, 30));
        }

        using var service = new OcrService();
        var result = await service.RecognizeAsync(bitmap, OcrLanguage.EnglishAndRussian);

        Assert.Contains("OCR", result.Text.ToUpperInvariant());
        Assert.Contains("ПРИВЕТ", result.Text.ToUpperInvariant());
    }
}
