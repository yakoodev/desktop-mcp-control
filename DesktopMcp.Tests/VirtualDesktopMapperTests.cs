using DesktopMcp.Core.Models;
using DesktopMcp.Core.Windows;

namespace DesktopMcp.Tests;

public class VirtualDesktopMapperTests
{
    [Fact]
    public void ToAbsolute_MapsNegativeVirtualCoordinates()
    {
        var virtualBounds = new ScreenRect(-1920, -200, 3840, 2160);
        var point = new ScreenPoint(-1920, -200);

        var (x, y) = VirtualDesktopMapper.ToAbsolute(virtualBounds, point);

        Assert.Equal(0, x);
        Assert.Equal(0, y);
    }

    [Fact]
    public void ToAbsolute_MapsBottomRightToMaxRange()
    {
        var virtualBounds = new ScreenRect(-1280, 0, 3200, 1080);
        var point = new ScreenPoint(1919, 1079);

        var (x, y) = VirtualDesktopMapper.ToAbsolute(virtualBounds, point);

        Assert.Equal(65535, x);
        Assert.Equal(65535, y);
    }

    [Fact]
    public void ToAbsolute_ClampsOutOfBoundsCoordinates()
    {
        var virtualBounds = new ScreenRect(0, 0, 1920, 1080);
        var point = new ScreenPoint(5000, -1000);

        var (x, y) = VirtualDesktopMapper.ToAbsolute(virtualBounds, point);

        Assert.Equal(65535, x);
        Assert.Equal(0, y);
    }
}
