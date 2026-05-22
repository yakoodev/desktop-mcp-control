using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Linux;

public static class LinuxDesktopCapabilityProbe
{
    public static bool IsWaylandSession()
    {
        var sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
        if (string.Equals(sessionType, "wayland", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
        return !string.IsNullOrWhiteSpace(waylandDisplay);
    }

    public static DesktopCapabilities BuildX11Capabilities()
    {
        var hasXdotool = LinuxCommandRunner.HasCommand("xdotool");
        var hasWindowTool = LinuxCommandRunner.HasCommand("wmctrl") || LinuxCommandRunner.HasCommand("xprop");
        var hasCaptureTool = LinuxCommandRunner.HasCommand("import")
            || LinuxCommandRunner.HasCommand("grim")
            || LinuxCommandRunner.HasCommand("gnome-screenshot")
            || LinuxCommandRunner.HasCommand("spectacle");

        return new DesktopCapabilities(
            Mouse: hasXdotool,
            Keyboard: hasXdotool,
            Capture: hasCaptureTool,
            WindowList: hasWindowTool || hasXdotool,
            Tray: true,
            GlobalHotkey: true);
    }

    public static DesktopCapabilities BuildWaylandCapabilities()
    {
        var hasCaptureTool = LinuxCommandRunner.HasCommand("grim")
            || LinuxCommandRunner.HasCommand("gnome-screenshot")
            || LinuxCommandRunner.HasCommand("spectacle");

        return new DesktopCapabilities(
            Mouse: false,
            Keyboard: false,
            Capture: hasCaptureTool,
            WindowList: false,
            Tray: true,
            GlobalHotkey: false);
    }
}
