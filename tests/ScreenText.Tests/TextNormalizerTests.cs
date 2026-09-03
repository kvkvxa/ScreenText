using ScreenText.Ocr;
using Xunit;

namespace ScreenText.Tests;

public sealed class TextNormalizerTests
{
    [Fact]
    public void NormalizesLineEndingsAndTrailingSpacesWithoutChangingContent()
    {
        var result = TextNormalizer.Normalize(" first  \rsecond\nthird\r\n");

        Assert.Equal("first\r\nsecond\r\nthird", result);
    }

    [Fact]
    public void RemovesNulAndUnsupportedControlCharacters()
    {
        var result = TextNormalizer.Normalize("hello\0\u0001\tworld");

        Assert.Equal("hello\tworld", result);
    }

    [Fact]
    public void PreservesParagraphBreaks()
    {
        var result = TextNormalizer.Normalize("one\n\ntwo");

        Assert.Equal("one\r\n\r\ntwo", result);
    }
}
