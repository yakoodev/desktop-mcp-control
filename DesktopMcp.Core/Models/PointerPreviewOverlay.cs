namespace DesktopMcp.Core.Models;

public sealed record PointerPreviewOverlay(
    ScreenPoint? ClickPoint = null,
    ScreenPoint? SwipeFrom = null,
    ScreenPoint? SwipeTo = null,
    string Color = "#FF7A18",
    float Alpha = 0.92f,
    int Thickness = 3,
    int PointRadius = 10,
    string? Label = null);
