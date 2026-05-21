namespace DesktopMcp.Core.Models;

public sealed record WindowInfo(
    string WindowId,
    string Title,
    string ProcessName,
    ScreenRect Bounds,
    bool IsVisible,
    bool IsForeground);
