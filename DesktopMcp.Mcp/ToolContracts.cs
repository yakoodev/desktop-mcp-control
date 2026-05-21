namespace DesktopMcp.Mcp;

public sealed class CaptureRegionArgs
{
    public int X { get; init; }

    public int Y { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }
}

public sealed class CaptureGridArgs
{
    public int StepPx { get; init; } = 50;

    public bool ShowLabels { get; init; } = true;

    public string LineColor { get; init; } = "#00FF88";

    public float LineAlpha { get; init; } = 0.45f;
}

public sealed class PointerOverlayStyleArgs
{
    public string Color { get; init; } = "#FF7A18";

    public float Alpha { get; init; } = 0.92f;

    public int Thickness { get; init; } = 3;

    public int PointRadius { get; init; } = 10;

    public string? Label { get; init; }
}
