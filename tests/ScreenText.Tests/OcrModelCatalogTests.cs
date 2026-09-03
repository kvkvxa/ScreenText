using System;
using System.IO;
using ScreenText.Ocr;
using Xunit;

namespace ScreenText.Tests;

public sealed class OcrModelCatalogTests
{
    [Fact]
    public void DiscoversLanguageModelsWithoutTreatingOsdAsTextLanguage()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ScreenTextTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllBytes(Path.Combine(directory, "eng.traineddata"), [1]);
            File.WriteAllBytes(Path.Combine(directory, "chi_sim.traineddata"), [1]);
            File.WriteAllBytes(Path.Combine(directory, "osd.traineddata"), [1]);

            var catalog = new OcrModelCatalog(directory);

            Assert.Equal(["chi_sim", "eng"], catalog.AvailableLanguageCodes);
            Assert.True(catalog.HasOsdModel);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void RejectsPathLikeModelCode()
    {
        var catalog = new OcrModelCatalog(Path.GetTempPath());

        Assert.Throws<ArgumentException>(() => catalog.GetModelPath("..\\eng"));
    }
}
