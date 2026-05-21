namespace DesktopMcp.Core.Models;

public sealed record CaptureRequest(
    CaptureTarget Target,
    string? DisplayId = null,
    string? WindowId = null,
    ScreenRect? Region = null,
    CaptureFormat Format = CaptureFormat.Png,
    int? Quality = null,
    GridOverlayOptions? Grid = null,
    PointerPreviewOverlay? PointerOverlay = null);
