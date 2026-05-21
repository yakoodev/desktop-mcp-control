using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Windows;

internal static class Win32Conversions
{
    internal static ScreenRect ToScreenRect(this NativeMethods.Rect rect)
    {
        return new ScreenRect(
            rect.Left,
            rect.Top,
            rect.Right - rect.Left,
            rect.Bottom - rect.Top);
    }
}
