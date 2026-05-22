using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using DesktopMcp.Core.Abstractions;
using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Linux.X11;

public sealed class X11DesktopAutomationBackend : IDesktopAutomationBackend
{
    private static readonly Regex XrandrConnectedRegex = new(
        @"^(?<name>\S+)\s+connected(?:\s+primary)?\s+(?<w>\d+)x(?<h>\d+)\+(?<x>-?\d+)\+(?<y>-?\d+)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex WmctrlRegex = new(
        @"^(?<id>0x[0-9a-fA-F]+)\s+\S+\s+(?<pid>\d+)\s+(?<x>-?\d+)\s+(?<y>-?\d+)\s+(?<w>\d+)\s+(?<h>\d+)\s+\S+\s*(?<title>.*)$",
        RegexOptions.Compiled);

    private static readonly Regex XdotoolShellLineRegex = new(
        @"^(?<key>[A-Z_]+)=(?<value>.*)$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private readonly DesktopCapabilities _capabilities;

    public X11DesktopAutomationBackend()
    {
        _capabilities = LinuxDesktopCapabilityProbe.BuildX11Capabilities();
    }

    public DesktopCapabilities GetCapabilities() => _capabilities;

    public VirtualDesktopInfo GetVirtualDesktopInfo()
    {
        var displays = TryGetDisplaysFromXrandr().ToList();
        if (displays.Count == 0)
        {
            displays.Add(new DisplayInfo("display-1", new ScreenRect(0, 0, 1920, 1080), true, 1));
        }

        var minX = displays.Min(d => d.Bounds.X);
        var minY = displays.Min(d => d.Bounds.Y);
        var maxRight = displays.Max(d => d.Bounds.Right);
        var maxBottom = displays.Max(d => d.Bounds.Bottom);
        var virtualBounds = new ScreenRect(minX, minY, maxRight - minX, maxBottom - minY);

        return new VirtualDesktopInfo(virtualBounds, displays);
    }

    public IReadOnlyList<WindowInfo> ListWindows(WindowQuery query)
    {
        if (LinuxCommandRunner.HasCommand("wmctrl"))
        {
            return ListWindowsWithWmctrl(query);
        }

        return ListWindowsWithXdotool(query);
    }

    public bool TryGetWindowBounds(string windowId, out ScreenRect bounds)
    {
        bounds = default;
        if (!LinuxCommandRunner.HasCommand("xwininfo"))
        {
            return false;
        }

        try
        {
            var output = LinuxCommandRunner.Run("xwininfo", $"-id {windowId}");
            var x = TryParseXwinInfoValue(output, "Absolute upper-left X:");
            var y = TryParseXwinInfoValue(output, "Absolute upper-left Y:");
            var width = TryParseXwinInfoValue(output, "Width:");
            var height = TryParseXwinInfoValue(output, "Height:");

            if (width <= 0 || height <= 0)
            {
                return false;
            }

            bounds = new ScreenRect(x, y, width, height);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void MoveMouse(ScreenPoint point)
    {
        EnsureTool("xdotool");
        LinuxCommandRunner.Run("xdotool", $"mousemove --sync {point.X} {point.Y}");
    }

    public void Click(ScreenPoint point, MouseButton button, int clickCount)
    {
        EnsureTool("xdotool");

        if (clickCount is < 1 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(clickCount), "clickCount must be 1 or 2.");
        }

        var buttonCode = button switch
        {
            MouseButton.Left => 1,
            MouseButton.Middle => 2,
            MouseButton.Right => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(button))
        };

        LinuxCommandRunner.Run(
            "xdotool",
            $"mousemove --sync {point.X} {point.Y} click --repeat {clickCount} --delay 60 {buttonCode}");
    }

    public void Scroll(ScreenPoint point, int deltaY, int deltaX)
    {
        EnsureTool("xdotool");
        LinuxCommandRunner.Run("xdotool", $"mousemove --sync {point.X} {point.Y}");

        if (deltaY != 0)
        {
            var button = deltaY > 0 ? 4 : 5;
            var repeat = Math.Max(1, Math.Abs(deltaY) / 120);
            LinuxCommandRunner.Run("xdotool", $"click --repeat {repeat} --delay 20 {button}");
        }

        if (deltaX != 0)
        {
            var button = deltaX > 0 ? 7 : 6;
            var repeat = Math.Max(1, Math.Abs(deltaX) / 120);
            LinuxCommandRunner.Run("xdotool", $"click --repeat {repeat} --delay 20 {button}");
        }
    }

    public void TypeText(string text)
    {
        EnsureTool("xdotool");
        ArgumentNullException.ThrowIfNull(text);
        var escaped = text.Replace("\"", "\\\"");
        LinuxCommandRunner.Run("xdotool", $"type --delay 2 -- \"{escaped}\"");
    }

    public void PressHotkey(IReadOnlyList<string> keys)
    {
        EnsureTool("xdotool");
        if (keys.Count == 0)
        {
            throw new ArgumentException("At least one key is required.", nameof(keys));
        }

        var chord = string.Join("+", keys.Select(MapHotkeyToken));
        LinuxCommandRunner.Run("xdotool", $"key --clearmodifiers {chord}");
    }

    public async Task DragDropAsync(
        ScreenPoint from,
        ScreenPoint to,
        MouseButton button,
        int durationMs,
        CancellationToken cancellationToken)
    {
        EnsureTool("xdotool");
        durationMs = Math.Clamp(durationMs, 100, 5000);

        var buttonCode = button switch
        {
            MouseButton.Left => 1,
            MouseButton.Middle => 2,
            MouseButton.Right => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(button))
        };

        LinuxCommandRunner.Run(
            "xdotool",
            $"mousemove --sync {from.X} {from.Y} mousedown {buttonCode}");

        var steps = Math.Max(4, durationMs / 16);
        for (var step = 1; step <= steps; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var progress = step / (double)steps;
            var x = (int)Math.Round(from.X + (to.X - from.X) * progress);
            var y = (int)Math.Round(from.Y + (to.Y - from.Y) * progress);
            LinuxCommandRunner.Run("xdotool", $"mousemove --sync {x} {y}");
            await Task.Delay(Math.Max(1, durationMs / steps), cancellationToken).ConfigureAwait(false);
        }

        LinuxCommandRunner.Run(
            "xdotool",
            $"mousemove --sync {to.X} {to.Y} mouseup {buttonCode}");
    }

    public ScreenPoint GetCursorPosition()
    {
        EnsureTool("xdotool");
        var output = LinuxCommandRunner.Run("xdotool", "getmouselocation --shell");
        var values = ParseShellPairs(output);
        var x = ParseInt(values, "X");
        var y = ParseInt(values, "Y");
        return new ScreenPoint(x, y);
    }

    public CaptureResult Capture(CaptureRequest request)
    {
        var cursor = GetCursorPosition();
        return request.Target switch
        {
            CaptureTarget.Display => CaptureDisplay(request, cursor),
            CaptureTarget.Window => CaptureWindow(request, cursor),
            CaptureTarget.Region => CaptureRegion(request, cursor),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Target))
        };
    }

    private CaptureResult CaptureDisplay(CaptureRequest request, ScreenPoint cursor)
    {
        var desktop = GetVirtualDesktopInfo();
        var display = string.IsNullOrWhiteSpace(request.DisplayId)
            ? desktop.Displays.FirstOrDefault(d => d.IsPrimary) ?? desktop.Displays.First()
            : desktop.Displays.FirstOrDefault(d => string.Equals(d.DisplayId, request.DisplayId, StringComparison.OrdinalIgnoreCase));

        if (display is null)
        {
            throw new ArgumentException($"Display not found: {request.DisplayId}");
        }

        var bytes = CaptureBytes(display.Bounds);
        var mime = request.Format == CaptureFormat.Jpeg ? "image/jpeg" : "image/png";
        return new CaptureResult(
            bytes,
            mime,
            display.Bounds.Width,
            display.Bounds.Height,
            display.Bounds,
            cursor,
            "display",
            display.DisplayId);
    }

    private CaptureResult CaptureWindow(CaptureRequest request, ScreenPoint cursor)
    {
        if (string.IsNullOrWhiteSpace(request.WindowId))
        {
            throw new ArgumentException("windowId is required for window capture.");
        }

        var bytes = CaptureWindowBytes(request.WindowId!);
        if (!TryGetWindowBounds(request.WindowId!, out var bounds))
        {
            bounds = new ScreenRect(0, 0, 0, 0);
        }

        var mime = request.Format == CaptureFormat.Jpeg ? "image/jpeg" : "image/png";
        return new CaptureResult(
            bytes,
            mime,
            Math.Max(0, bounds.Width),
            Math.Max(0, bounds.Height),
            bounds,
            cursor,
            "window",
            request.WindowId);
    }

    private CaptureResult CaptureRegion(CaptureRequest request, ScreenPoint cursor)
    {
        if (request.Region is not { } region || region.IsEmpty)
        {
            throw new ArgumentException("region is required for region capture.");
        }

        var bytes = CaptureBytes(region);
        var mime = request.Format == CaptureFormat.Jpeg ? "image/jpeg" : "image/png";
        return new CaptureResult(
            bytes,
            mime,
            region.Width,
            region.Height,
            region,
            cursor,
            "region",
            null);
    }

    private static IReadOnlyList<DisplayInfo> TryGetDisplaysFromXrandr()
    {
        if (!LinuxCommandRunner.HasCommand("xrandr"))
        {
            return [];
        }

        var output = LinuxCommandRunner.Run("xrandr", "--query --current");
        var displays = new List<DisplayInfo>();
        var index = 0;

        foreach (Match match in XrandrConnectedRegex.Matches(output))
        {
            index++;
            var id = match.Groups["name"].Value;
            var width = int.Parse(match.Groups["w"].Value, CultureInfo.InvariantCulture);
            var height = int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture);
            var x = int.Parse(match.Groups["x"].Value, CultureInfo.InvariantCulture);
            var y = int.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture);
            var isPrimary = output.Contains($"{id} connected primary ", StringComparison.Ordinal);
            displays.Add(new DisplayInfo(id, new ScreenRect(x, y, width, height), isPrimary || index == 1, 1d));
        }

        return displays;
    }

    private IReadOnlyList<WindowInfo> ListWindowsWithWmctrl(WindowQuery query)
    {
        var output = LinuxCommandRunner.Run("wmctrl", "-lpG");
        var windows = new List<WindowInfo>();

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = WmctrlRegex.Match(line.Trim());
            if (!match.Success)
            {
                continue;
            }

            var title = match.Groups["title"].Value.Trim();
            var processName = ResolveProcessName(match.Groups["pid"].Value);
            if (!MatchesQuery(query, title, processName))
            {
                continue;
            }

            var x = int.Parse(match.Groups["x"].Value, CultureInfo.InvariantCulture);
            var y = int.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture);
            var w = int.Parse(match.Groups["w"].Value, CultureInfo.InvariantCulture);
            var h = int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture);

            windows.Add(
                new WindowInfo(
                    match.Groups["id"].Value,
                    title,
                    processName,
                    new ScreenRect(x, y, w, h),
                    true,
                    false));
        }

        return windows;
    }

    private IReadOnlyList<WindowInfo> ListWindowsWithXdotool(WindowQuery query)
    {
        EnsureTool("xdotool");
        var output = LinuxCommandRunner.Run("xdotool", "search --onlyvisible --name .");
        var windows = new List<WindowInfo>();

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var idRaw = line.Trim();
            if (!long.TryParse(idRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericId))
            {
                continue;
            }

            var windowId = $"0x{numericId:X}";
            var title = SafeRun("xdotool", $"getwindowname {idRaw}")?.Trim() ?? string.Empty;
            var pidText = SafeRun("xdotool", $"getwindowpid {idRaw}")?.Trim();
            var processName = ResolveProcessName(pidText ?? string.Empty);
            if (!MatchesQuery(query, title, processName))
            {
                continue;
            }

            if (!TryGetWindowBounds(windowId, out var bounds))
            {
                continue;
            }

            windows.Add(new WindowInfo(windowId, title, processName, bounds, true, false));
        }

        return windows;
    }

    private static bool MatchesQuery(WindowQuery query, string title, string processName)
    {
        if (!string.IsNullOrWhiteSpace(query.TitleContains) &&
            title.IndexOf(query.TitleContains, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.ProcessName) &&
            processName.IndexOf(query.ProcessName, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        return true;
    }

    private static string ResolveProcessName(string pidText)
    {
        if (!int.TryParse(pidText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid))
        {
            return string.Empty;
        }

        try
        {
            using var process = Process.GetProcessById(pid);
            return process.ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static int TryParseXwinInfoValue(string output, string prefix)
    {
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var raw = trimmed[prefix.Length..].Trim();
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
        }

        return 0;
    }

    private static Dictionary<string, string> ParseShellPairs(string text)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in XdotoolShellLineRegex.Matches(text))
        {
            values[match.Groups["key"].Value.Trim()] = match.Groups["value"].Value.Trim();
        }

        return values;
    }

    private static int ParseInt(Dictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var raw) ||
            !int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidOperationException($"Unable to parse integer value for key '{key}'.");
        }

        return parsed;
    }

    private byte[] CaptureBytes(ScreenRect bounds)
    {
        if (LinuxCommandRunner.HasCommand("import"))
        {
            return LinuxCommandRunner.RunForBytes(
                "import",
                $"-window root -silent -crop {bounds.Width}x{bounds.Height}+{bounds.X}+{bounds.Y} png:-");
        }

        if (LinuxCommandRunner.HasCommand("grim"))
        {
            var bytes = LinuxCommandRunner.RunForBytes("grim", $"-g \"{bounds.X},{bounds.Y} {bounds.Width}x{bounds.Height}\" -");
            if (bytes.Length > 0)
            {
                return bytes;
            }
        }

        return CaptureWithFileFallback(bounds);
    }

    private byte[] CaptureWindowBytes(string windowId)
    {
        if (LinuxCommandRunner.HasCommand("import"))
        {
            return LinuxCommandRunner.RunForBytes("import", $"-window {windowId} -silent png:-");
        }

        throw new InvalidOperationException("Window capture requires ImageMagick 'import' on Linux.");
    }

    private byte[] CaptureWithFileFallback(ScreenRect bounds)
    {
        var desktopBounds = GetVirtualDesktopInfo().VirtualBounds;
        var isFullDesktop = bounds.X == desktopBounds.X &&
            bounds.Y == desktopBounds.Y &&
            bounds.Width == desktopBounds.Width &&
            bounds.Height == desktopBounds.Height;
        if (!isFullDesktop)
        {
            throw new InvalidOperationException(
                "Region/window crop capture requires ImageMagick 'import' or grim on Linux.");
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"desktop-mcp-{Guid.NewGuid():N}.png");

        try
        {
            if (LinuxCommandRunner.HasCommand("gnome-screenshot"))
            {
                LinuxCommandRunner.Run("gnome-screenshot", $"-f \"{tempPath}\"");
            }
            else if (LinuxCommandRunner.HasCommand("spectacle"))
            {
                LinuxCommandRunner.Run("spectacle", $"-b -n -o \"{tempPath}\"");
            }
            else
            {
                throw new InvalidOperationException(
                    "No supported screenshot tool found. Install ImageMagick 'import', grim, gnome-screenshot, or spectacle.");
            }

            return File.ReadAllBytes(tempPath);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static string MapHotkeyToken(string key)
    {
        return key.Trim().ToUpperInvariant() switch
        {
            "CTRL" or "CONTROL" => "ctrl",
            "ALT" => "alt",
            "SHIFT" => "shift",
            "WIN" or "WINDOWS" or "SUPER" => "super",
            "ENTER" => "Return",
            "ESC" or "ESCAPE" => "Escape",
            "SPACE" => "space",
            "TAB" => "Tab",
            "BACKSPACE" => "BackSpace",
            "DELETE" => "Delete",
            "LEFT" => "Left",
            "RIGHT" => "Right",
            "UP" => "Up",
            "DOWN" => "Down",
            _ when key.Length == 1 => key.ToLowerInvariant(),
            _ => key
        };
    }

    private static void EnsureTool(string tool)
    {
        if (LinuxCommandRunner.HasCommand(tool))
        {
            return;
        }

        throw new InvalidOperationException($"Linux backend requires '{tool}' to be installed.");
    }

    private static string? SafeRun(string command, string args)
    {
        try
        {
            return LinuxCommandRunner.Run(command, args);
        }
        catch
        {
            return null;
        }
    }
}
