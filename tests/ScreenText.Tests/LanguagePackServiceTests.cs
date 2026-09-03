using System;
using System.IO;
using ScreenText.Ocr;
using Xunit;

namespace ScreenText.Tests;

public sealed class LanguagePackServiceTests
{
    [Fact]
    public void ImportsModelIntoUserDataAndWritesChecksumManifest()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ScreenTextTests", Guid.NewGuid().ToString("N"));
        var sourcePath = Path.Combine(directory, "spa.traineddata");
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(sourcePath, [1, 2, 3]);

        try
        {
            var service = new LanguagePackService(Path.Combine(directory, "models"));
            var result = service.Import(sourcePath);
            var catalog = new OcrModelCatalog(Path.Combine(directory, "bundled"), service.ModelsPath);

            Assert.Equal("spa", result.Code);
            Assert.True(catalog.HasModel("spa"));
            catalog.VerifyIntegrity(["spa"]);
            Assert.Contains("spa.traineddata", File.ReadAllText(Path.Combine(service.ModelsPath, "SHA256SUMS.txt")));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void RejectsNonTesseractFiles()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ScreenTextTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var sourcePath = Path.Combine(directory, "spa.txt");
        File.WriteAllText(sourcePath, "not a model");

        try
        {
            Assert.Throws<InvalidDataException>(() => new LanguagePackService(Path.Combine(directory, "models")).Import(sourcePath));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
