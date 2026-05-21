namespace DesktopMcp.Core.Models;

public sealed record VirtualDesktopInfo(
    ScreenRect VirtualBounds,
    IReadOnlyList<DisplayInfo> Displays);
