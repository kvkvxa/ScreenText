using ScreenText.Capture;
using Xunit;

namespace ScreenText.Tests;

public sealed class PhysicalRectTests
{
    [Fact]
    public void FromPointsNormalizesReverseDrag()
    {
        var rect = PhysicalRect.FromPoints(new PhysicalPoint(500, 600), new PhysicalPoint(-200, 100));
        Assert.Equal(new PhysicalRect(-200, 100, 500, 600), rect);
    }

    [Fact]
    public void IntersectClipsNegativeCoordinateMonitor()
    {
        var monitor = new PhysicalRect(-1920, 0, 0, 1080);
        var selection = new PhysicalRect(-200, 100, 500, 600);
        Assert.Equal(new PhysicalRect(-200, 100, 0, 600), monitor.Intersect(selection));
    }

    [Fact]
    public void IntersectClipsPrimaryMonitor()
    {
        var monitor = new PhysicalRect(0, 0, 2560, 1440);
        var selection = new PhysicalRect(-200, 100, 500, 600);
        Assert.Equal(new PhysicalRect(0, 100, 500, 600), monitor.Intersect(selection));
    }

    [Fact]
    public void DipConversionUsesExplicitScale()
    {
        var rect = DipRect.FromPhysical(new PhysicalRect(150, 300, 450, 600), 1.5);
        Assert.Equal(new DipRect(100, 200, 300, 400), rect);
        Assert.Equal(new PhysicalPoint(150, 300), new DipPoint(100, 200).ToPhysical(1.5));
    }
}
