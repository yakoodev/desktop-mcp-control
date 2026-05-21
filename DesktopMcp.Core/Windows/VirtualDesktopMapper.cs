using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Windows;

public static class VirtualDesktopMapper
{
    public static (int normalizedX, int normalizedY) ToAbsolute(ScreenRect virtualBounds, ScreenPoint point)
    {
        if (virtualBounds.Width <= 0 || virtualBounds.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(virtualBounds), "Virtual desktop bounds are invalid.");
        }

        var maxX = Math.Max(1, virtualBounds.Width - 1);
        var maxY = Math.Max(1, virtualBounds.Height - 1);

        var clampedX = Math.Clamp(point.X, virtualBounds.X, virtualBounds.Right - 1);
        var clampedY = Math.Clamp(point.Y, virtualBounds.Y, virtualBounds.Bottom - 1);

        var normalizedX = (int)Math.Round((clampedX - virtualBounds.X) * (65535d / maxX));
        var normalizedY = (int)Math.Round((clampedY - virtualBounds.Y) * (65535d / maxY));

        return (normalizedX, normalizedY);
    }
}
