using System.Drawing;
using System.Drawing.Imaging;
using ScreenText.Ocr;
using Xunit;

namespace ScreenText.Tests;

public sealed class OcrImagePreprocessorTests
{
    [Fact]
    public void UpscalesSmallDarkCaptureAndKeepsItInMemory()
    {
        using var source = new Bitmap(200, 100);
        using (var graphics = Graphics.FromImage(source))
        using (var font = new Font("Arial", 16, FontStyle.Bold))
        {
            graphics.Clear(Color.FromArgb(24, 24, 24));
            graphics.DrawString("Small UI text", font, Brushes.White, new PointF(8, 30));
        }

        using var prepared = OcrImagePreprocessor.Prepare(source);

        Assert.Equal(400, prepared.Width);
        Assert.Equal(200, prepared.Height);
        Assert.Equal(PixelFormat.Format24bppRgb, prepared.PixelFormat);
        Assert.True(prepared.GetPixel(0, 0).R > 200);
    }

    [Fact]
    public void DoesNotUpscaleLargeCaptureWithoutReason()
    {
        using var source = new Bitmap(1600, 1000);

        using var prepared = OcrImagePreprocessor.Prepare(source);

        Assert.Equal(source.Size, prepared.Size);
    }
}
