using System.Drawing;
using System.IO;
using System;
using System.Threading.Tasks;
using ScreenText.Capture;
using ScreenText.Ocr;
using ScreenText.Settings;
using Xunit;

namespace ScreenText.Tests;

public sealed class RobustnessTests
{
    [Fact]
    public void EmptyCaptureRectangleHasTypedFailure()
    {
        var capture = new ScreenCapture();
        Assert.Throws<ScreenCaptureException>(() => capture.Capture(new PhysicalRect(10, 10, 10, 20)));
    }

    [Fact]
    public async Task MissingOcrModelsHaveTypedFailure()
    {
        using var bitmap = new Bitmap(20, 20);
        using var service = new OcrService(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        await Assert.ThrowsAsync<OcrModelMissingException>(() => service.RecognizeAsync(bitmap, OcrLanguage.English));
    }

    [Fact]
    public async Task TamperedOcrModelHasTypedIntegrityFailure()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ScreenTextTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "eng.traineddata"), "tampered model");
        File.WriteAllText(Path.Combine(directory, "SHA256SUMS.txt"),
            "0000000000000000000000000000000000000000000000000000000000000000  eng.traineddata");

        try
        {
            using var bitmap = new Bitmap(20, 20);
            using var service = new OcrService(directory);

            await Assert.ThrowsAsync<OcrModelIntegrityException>(() => service.RecognizeAsync(bitmap, OcrLanguage.English));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void CorruptSettingsFallBackAndPreserveCorruptFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ScreenTextTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ this is not valid json");

        try
        {
            var settings = new SettingsService(path).Load();
            Assert.Equal("eng+rus", settings.Language);
            Assert.False(File.Exists(path));
            Assert.Single(Directory.GetFiles(directory, "settings.json.corrupt-*"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
