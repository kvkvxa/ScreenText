using ScreenText.Ocr;
using Xunit;

namespace ScreenText.Tests;

public sealed class OcrScriptRouterTests
{
    [Fact]
    public void ResolvesDifferentScriptsToAvailableModels()
    {
        var models = OcrScriptRouter.ResolveModels(["Latin", "Han"], ["eng", "chi_sim", "rus"]);

        Assert.Equal(["eng", "chi_sim"], models);
    }

    [Fact]
    public void DoesNotFallbackToAnUnrelatedModel()
    {
        Assert.Throws<OcrUnsupportedScriptException>(() =>
            OcrScriptRouter.ResolveModels(["Han"], ["eng", "rus"]));
    }

    [Fact]
    public void RejectsUnknownScriptInsteadOfGuessing()
    {
        Assert.Throws<OcrUnsupportedScriptException>(() =>
            OcrScriptRouter.ResolveModels(["UnknownScript"], ["eng", "rus"]));
    }
}
