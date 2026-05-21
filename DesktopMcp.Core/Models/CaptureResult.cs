namespace DesktopMcp.Core.Models;

public sealed record CaptureResult(
    byte[] ImageBytes,
    string MimeType,
    int Width,
    int Height,
    ScreenRect Bounds,
    ScreenPoint CursorPosition,
    string Target,
    string? SourceId);
