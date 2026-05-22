using DesktopMcp.Core.Abstractions;
using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Linux.Wayland;

public sealed class WaylandPortalDesktopAutomationBackend : IDesktopAutomationBackend
{
    private readonly DesktopCapabilities _capabilities;

    public WaylandPortalDesktopAutomationBackend()
    {
        _capabilities = LinuxDesktopCapabilityProbe.BuildWaylandCapabilities();
    }

    public DesktopCapabilities GetCapabilities() => _capabilities;

    public VirtualDesktopInfo GetVirtualDesktopInfo()
    {
        var x11Displays = TryGetFallbackDisplays();
        if (x11Displays.Count == 0)
        {
            x11Displays.Add(new DisplayInfo("display-1", new ScreenRect(0, 0, 1920, 1080), true, 1d));
        }

        var minX = x11Displays.Min(d => d.Bounds.X);
        var minY = x11Displays.Min(d => d.Bounds.Y);
        var maxRight = x11Displays.Max(d => d.Bounds.Right);
        var maxBottom = x11Displays.Max(d => d.Bounds.Bottom);
        var virtualBounds = new ScreenRect(minX, minY, maxRight - minX, maxBottom - minY);

        return new VirtualDesktopInfo(virtualBounds, x11Displays);
    }

    public IReadOnlyList<WindowInfo> ListWindows(WindowQuery query)
    {
        return [];
    }

    public bool TryGetWindowBounds(string windowId, out ScreenRect bounds)
    {
        bounds = default;
        return false;
    }

    public void MoveMouse(ScreenPoint point)
    {
        throw new NotSupportedException("Wayland pointer injection requires compositor-specific portal support.");
    }

    public void Click(ScreenPoint point, MouseButton button, int clickCount)
    {
        throw new NotSupportedException("Wayland pointer injection requires compositor-specific portal support.");
    }

    public void Scroll(ScreenPoint point, int deltaY, int deltaX)
    {
        throw new NotSupportedException("Wayland pointer injection requires compositor-specific portal support.");
    }

    public void TypeText(string text)
    {
        throw new NotSupportedException("Wayland keyboard injection requires compositor-specific portal support.");
    }

    public void PressHotkey(IReadOnlyList<string> keys)
    {
        throw new NotSupportedException("Wayland keyboard injection requires compositor-specific portal support.");
    }

    public Task DragDropAsync(
        ScreenPoint from,
        ScreenPoint to,
        MouseButton button,
        int durationMs,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Wayland pointer injection requires compositor-specific portal support.");
    }

    public ScreenPoint GetCursorPosition()
    {
        return new ScreenPoint(0, 0);
    }

    public CaptureResult Capture(CaptureRequest request)
    {
        if (!_capabilities.Capture)
        {
            throw new InvalidOperationException(
                "Wayland capture backend is unavailable. Install grim, gnome-screenshot, or spectacle.");
        }

        var bounds = ResolveBounds(request);
        var bytes = CaptureWaylandBytes(bounds, request.Target);
        return new CaptureResult(
            bytes,
            request.Format == CaptureFormat.Jpeg ? "image/jpeg" : "image/png",
            bounds.Width,
            bounds.Height,
            bounds,
            new ScreenPoint(0, 0),
            request.Target.ToString().ToLowerInvariant(),
            request.Target == CaptureTarget.Display ? request.DisplayId : null);
    }

    private ScreenRect ResolveBounds(CaptureRequest request)
    {
        return request.Target switch
        {
            CaptureTarget.Display => ResolveDisplayBounds(request.DisplayId),
            CaptureTarget.Region when request.Region is { } region && !region.IsEmpty => region,
            CaptureTarget.Region => throw new ArgumentException("region is required for region capture."),
            CaptureTarget.Window => throw new InvalidOperationException("Wayland window capture is not available in this backend."),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Target))
        };
    }

    private ScreenRect ResolveDisplayBounds(string? displayId)
    {
        var desktop = GetVirtualDesktopInfo();
        var display = string.IsNullOrWhiteSpace(displayId)
            ? desktop.Displays.FirstOrDefault(d => d.IsPrimary) ?? desktop.Displays.First()
            : desktop.Displays.FirstOrDefault(d => string.Equals(d.DisplayId, displayId, StringComparison.OrdinalIgnoreCase));

        if (display is null)
        {
            throw new ArgumentException($"Display not found: {displayId}");
        }

        return display.Bounds;
    }

    private static byte[] CaptureWaylandBytes(ScreenRect bounds, CaptureTarget target)
    {
        if (LinuxCommandRunner.HasCommand("grim"))
        {
            var args = target == CaptureTarget.Region
                ? $"-g \"{bounds.X},{bounds.Y} {bounds.Width}x{bounds.Height}\" -"
                : "-";
            return LinuxCommandRunner.RunForBytes("grim", args);
        }

        if (target == CaptureTarget.Region)
        {
            throw new InvalidOperationException("Wayland region capture requires grim.");
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"desktop-mcp-wayland-{Guid.NewGuid():N}.png");
        try
        {
            if (LinuxCommandRunner.HasCommand("gnome-screenshot"))
            {
                LinuxCommandRunner.Run("gnome-screenshot", $"-f \"{tempPath}\"");
                return File.ReadAllBytes(tempPath);
            }

            if (LinuxCommandRunner.HasCommand("spectacle"))
            {
                LinuxCommandRunner.Run("spectacle", $"-b -n -o \"{tempPath}\"");
                return File.ReadAllBytes(tempPath);
            }

            throw new InvalidOperationException("No compatible Wayland capture tool is available.");
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static List<DisplayInfo> TryGetFallbackDisplays()
    {
        if (!LinuxCommandRunner.HasCommand("xrandr"))
        {
            return [];
        }

        var output = LinuxCommandRunner.Run("xrandr", "--query --current");
        var displays = new List<DisplayInfo>();
        var regex = new System.Text.RegularExpressions.Regex(
            @"^(?<name>\S+)\s+connected(?:\s+primary)?\s+(?<w>\d+)x(?<h>\d+)\+(?<x>-?\d+)\+(?<y>-?\d+)",
            System.Text.RegularExpressions.RegexOptions.Multiline);

        foreach (System.Text.RegularExpressions.Match match in regex.Matches(output))
        {
            var id = match.Groups["name"].Value;
            var width = int.Parse(match.Groups["w"].Value);
            var height = int.Parse(match.Groups["h"].Value);
            var x = int.Parse(match.Groups["x"].Value);
            var y = int.Parse(match.Groups["y"].Value);
            var isPrimary = output.Contains($"{id} connected primary ", StringComparison.Ordinal);
            displays.Add(new DisplayInfo(id, new ScreenRect(x, y, width, height), isPrimary, 1d));
        }

        return displays;
    }
}
