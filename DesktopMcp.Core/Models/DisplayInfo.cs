namespace DesktopMcp.Core.Models;

public sealed record DisplayInfo(
    string DisplayId,
    ScreenRect Bounds,
    bool IsPrimary,
    double DpiScale);
