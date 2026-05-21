namespace DesktopMcp.Core.Models;

public sealed record WindowQuery(
    string? TitleContains = null,
    string? ProcessName = null,
    bool OnlyVisible = true);
