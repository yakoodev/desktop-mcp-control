using Avalonia.Controls;
using System.Runtime.InteropServices;

namespace DesktopMcp.App.Views;

internal static class NativeWindowRegion
{
    public static void ApplyRoundedCorners(Window window, double radius)
    {
        window.Opened += (_, _) => Apply(window, radius);
    }

    private static void Apply(Window window, double radius)
    {
        var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var scale = window.RenderScaling;
        var width = Math.Max(1, (int)Math.Round(window.Bounds.Width * scale));
        var height = Math.Max(1, (int)Math.Round(window.Bounds.Height * scale));
        var diameter = Math.Max(1, (int)Math.Round(radius * 2 * scale));

        var region = CreateRoundRectRgn(0, 0, width + 1, height + 1, diameter, diameter);
        if (region == IntPtr.Zero)
        {
            return;
        }

        // Windows owns the region handle after a successful SetWindowRgn call.
        if (SetWindowRgn(handle, region, true) == 0)
        {
            DeleteObject(region);
        }
    }

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(
        int left,
        int top,
        int right,
        int bottom,
        int ellipseWidth,
        int ellipseHeight);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool redraw);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);
}
