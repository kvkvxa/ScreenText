using System;
using ScreenText.Ocr;
using Xunit;

namespace ScreenText.Tests;

public sealed class OcrLanguageTests
{
    [Theory]
    [InlineData("eng", "eng")]
    [InlineData("rus", "rus")]
    [InlineData("eng+rus", "eng+rus")]
    [InlineData("rus+eng", "rus+eng")]
    [InlineData("spa+chi_sim", "spa+chi_sim")]
    public void ParsesArbitrarySafeLanguageCodes(string value, string tessCode)
    {
        var language = OcrLanguageParser.Parse(value);
        Assert.Equal(tessCode, OcrLanguageParser.ToTesseractCode(language));
    }

    [Fact]
    public void ParsesAutoWithoutPretendingItIsATesseractLanguage()
    {
        var language = OcrLanguageParser.Parse("auto");

        Assert.True(language.IsAuto);
        Assert.Throws<ArgumentException>(() => OcrLanguageParser.ToTesseractCode(language));
    }

    [Fact]
    public void ValidButUnbundledLanguageIsNotSilentlyReplaced()
    {
        Assert.Equal("deu", OcrLanguageParser.Parse("deu").Code);
    }

    [Theory]
    [InlineData("../../eng")]
    [InlineData("eng+auto")]
    [InlineData("not a model")]
    public void InvalidLanguageFallsBackToSafeBundledDefault(string value)
    {
        Assert.Equal("eng+rus", OcrLanguageParser.Parse(value).Code);
    }
}
