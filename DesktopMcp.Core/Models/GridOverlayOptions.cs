namespace DesktopMcp.Core.Models;

public sealed record GridOverlayOptions(
    int StepPx = 50,
    bool ShowLabels = true,
    string LineColor = "#00FF88",
    float LineAlpha = 0.45f);
