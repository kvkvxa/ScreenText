using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ScreenText.Ocr;

public static class OcrImagePreprocessor
{
    public static Bitmap Prepare(Bitmap source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var scale = SelectScale(source.Width, source.Height);
        var isDark = HasDarkBackground(source);
        var prepared = new Bitmap(source.Width * scale, source.Height * scale, PixelFormat.Format24bppRgb);

        using var graphics = Graphics.FromImage(prepared);
        using var attributes = new ImageAttributes();
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        attributes.SetColorMatrix(CreateGrayscaleMatrix());
        graphics.DrawImage(source,
            new Rectangle(0, 0, prepared.Width, prepared.Height),
            0, 0, source.Width, source.Height,
            GraphicsUnit.Pixel,
            attributes);

        if (isDark) Invert(prepared);

        return prepared;
    }

    private static int SelectScale(int width, int height)
    {
        var pixels = (long)width * height;
        return Math.Min(width, height) < 900 && pixels <= 6_000_000 ? 2 : 1;
    }

    private static bool HasDarkBackground(Bitmap bitmap)
    {
        var stepX = Math.Max(1, bitmap.Width / 24);
        var stepY = Math.Max(1, bitmap.Height / 24);
        long luminanceSum = 0;
        var samples = 0;

        for (var y = 0; y < bitmap.Height; y += stepY)
        {
            for (var x = 0; x < bitmap.Width; x += stepX)
            {
                var color = bitmap.GetPixel(x, y);
                luminanceSum += (299L * color.R + 587L * color.G + 114L * color.B) / 1000;
                samples++;
            }
        }

        return samples > 0 && luminanceSum / samples < 128;
    }

    private static ColorMatrix CreateGrayscaleMatrix()
    {
        return new ColorMatrix([
            [0.299f, 0.587f, 0.114f, 0, 0],
            [0.299f, 0.587f, 0.114f, 0, 0],
            [0.299f, 0.587f, 0.114f, 0, 0],
            [0, 0, 0, 1, 0],
            [0, 0, 0, 0, 1]]);
    }

    private static void Invert(Bitmap bitmap)
    {
        var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rectangle, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        try
        {
            var stride = Math.Abs(data.Stride);
            var bytes = new byte[stride * bitmap.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            for (var index = 0; index < bytes.Length; index++) bytes[index] = (byte)(255 - bytes[index]);
            Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}
