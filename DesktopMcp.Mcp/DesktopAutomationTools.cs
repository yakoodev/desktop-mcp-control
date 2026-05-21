using System.ComponentModel;
using System.Text.Json;
using DesktopMcp.Core.Abstractions;
using DesktopMcp.Core.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DesktopMcp.Mcp;

[McpServerToolType]
public sealed class DesktopAutomationTools
{
    private readonly IDesktopAutomationController _controller;

    public DesktopAutomationTools(IDesktopAutomationController controller)
    {
        _controller = controller;
    }

    [McpServerTool(Name = "desktop.get_displays"), Description("Returns virtual desktop bounds and physical display list.")]
    public async Task<CallToolResult> GetDisplays(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _controller.GetDisplaysAsync(cancellationToken).ConfigureAwait(false);
            return Ok(
                new
                {
                    virtualBounds = result.VirtualBounds,
                    displays = result.Displays
                },
                "Displays enumerated.");
        }
        catch (Exception ex)
        {
            return Error("Failed to get displays.", ex);
        }
    }

    [McpServerTool(Name = "desktop.list_windows"), Description("Lists top-level windows for targeting screenshots and input.")]
    public async Task<CallToolResult> ListWindows(
        [Description("Optional case-insensitive part of window title.")] string? titleContains = null,
        [Description("Optional process name filter.")] string? processName = null,
        [Description("Whether to return only visible windows.")] bool onlyVisible = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new WindowQuery(titleContains, processName, onlyVisible);
            var windows = await _controller.ListWindowsAsync(query, cancellationToken).ConfigureAwait(false);
            return Ok(new { windows }, $"Found {windows.Count} windows.");
        }
        catch (Exception ex)
        {
            return Error("Failed to list windows.", ex);
        }
    }

    [McpServerTool(Name = "desktop.mouse_move"), Description("Moves mouse cursor to global virtual desktop coordinates.")]
    public async Task<CallToolResult> MouseMove(
        int x,
        int y,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cursor = await _controller.MouseMoveAsync(new ScreenPoint(x, y), cancellationToken).ConfigureAwait(false);
            return Ok(new { cursor }, "Mouse moved.");
        }
        catch (Exception ex)
        {
            return Error("Failed to move mouse.", ex);
        }
    }

    [McpServerTool(Name = "desktop.mouse_click"), Description("Moves cursor and performs mouse click.")]
    public async Task<CallToolResult> MouseClick(
        int x,
        int y,
        string button = "left",
        int clickCount = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cursor = await _controller.MouseClickAsync(
                    new ScreenPoint(x, y),
                    ParseMouseButton(button),
                    clickCount,
                    cancellationToken)
                .ConfigureAwait(false);

            return Ok(new { cursor, button, clickCount }, "Mouse click executed.");
        }
        catch (Exception ex)
        {
            return Error("Failed to click mouse.", ex);
        }
    }

    [McpServerTool(Name = "desktop.mouse_scroll"), Description("Moves cursor and scrolls at target coordinates.")]
    public async Task<CallToolResult> MouseScroll(
        int x,
        int y,
        int deltaY,
        int deltaX = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cursor = await _controller.MouseScrollAsync(
                    new ScreenPoint(x, y),
                    deltaY,
                    deltaX,
                    cancellationToken)
                .ConfigureAwait(false);

            return Ok(new { cursor, deltaY, deltaX }, "Scroll executed.");
        }
        catch (Exception ex)
        {
            return Error("Failed to scroll mouse.", ex);
        }
    }

    [McpServerTool(Name = "desktop.keyboard_type"), Description("Types text into currently focused window.")]
    public async Task<CallToolResult> KeyboardType(
        string text,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _controller.KeyboardTypeAsync(text, cancellationToken).ConfigureAwait(false);
            return Ok(new { textLength = text.Length }, "Text typed.");
        }
        catch (Exception ex)
        {
            return Error("Failed to type text.", ex);
        }
    }

    [McpServerTool(Name = "desktop.keyboard_hotkey"), Description("Sends a keyboard chord, e.g. [\"CTRL\",\"V\"].")]
    public async Task<CallToolResult> KeyboardHotkey(
        string[] keys,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _controller.KeyboardHotkeyAsync(keys, cancellationToken).ConfigureAwait(false);
            return Ok(new { keys }, "Hotkey sent.");
        }
        catch (Exception ex)
        {
            return Error("Failed to send hotkey.", ex);
        }
    }

    [McpServerTool(Name = "desktop.drag_drop"), Description("Performs drag-and-drop between global coordinates.")]
    public async Task<CallToolResult> DragDrop(
        int fromX,
        int fromY,
        int toX,
        int toY,
        string button = "left",
        int durationMs = 350,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _controller.DragDropAsync(
                    new ScreenPoint(fromX, fromY),
                    new ScreenPoint(toX, toY),
                    ParseMouseButton(button),
                    durationMs,
                    cancellationToken)
                .ConfigureAwait(false);

            return Ok(new { fromX, fromY, toX, toY, button, durationMs }, "Drag and drop complete.");
        }
        catch (Exception ex)
        {
            return Error("Failed to perform drag and drop.", ex);
        }
    }

    [McpServerTool(Name = "desktop.capture"), Description("Captures display, window, or screen region with optional coordinate grid.")]
    public async Task<CallToolResult> Capture(
        string target,
        string? displayId = null,
        string? windowId = null,
        CaptureRegionArgs? region = null,
        string format = "png",
        int? quality = null,
        CaptureGridArgs? grid = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = BuildCaptureRequest(target, displayId, windowId, region, format, quality, grid, overlay: null);
            var shot = await _controller.CaptureAsync(request, cancellationToken).ConfigureAwait(false);
            return BuildCaptureResult(shot, additionalStructuredContent: null, "Capture completed.");
        }
        catch (Exception ex)
        {
            return Error("Failed to capture screenshot.", ex);
        }
    }

    [McpServerTool(Name = "desktop.predict_click"), Description("Returns a screenshot preview with grid and marker where click will happen. Does not execute click.")]
    public async Task<CallToolResult> PredictClick(
        int x,
        int y,
        string target = "display",
        string? displayId = null,
        string? windowId = null,
        CaptureRegionArgs? region = null,
        string format = "png",
        int? quality = null,
        CaptureGridArgs? grid = null,
        PointerOverlayStyleArgs? style = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var overlay = BuildClickOverlay(new ScreenPoint(x, y), style);
            var request = BuildCaptureRequest(target, displayId, windowId, region, format, quality, grid, overlay);
            var shot = await _controller.CaptureAsync(request, cancellationToken).ConfigureAwait(false);
            return BuildCaptureResult(
                shot,
                new
                {
                    predictAction = "click",
                    point = new { x, y }
                },
                "Predict click preview rendered.");
        }
        catch (Exception ex)
        {
            return Error("Failed to render click prediction.", ex);
        }
    }

    [McpServerTool(Name = "desktop.predict_swipe"), Description("Returns a screenshot preview with grid and swipe path line. Does not execute swipe.")]
    public async Task<CallToolResult> PredictSwipe(
        int fromX,
        int fromY,
        int toX,
        int toY,
        string target = "display",
        string? displayId = null,
        string? windowId = null,
        CaptureRegionArgs? region = null,
        string format = "png",
        int? quality = null,
        CaptureGridArgs? grid = null,
        PointerOverlayStyleArgs? style = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var overlay = BuildSwipeOverlay(new ScreenPoint(fromX, fromY), new ScreenPoint(toX, toY), style);
            var request = BuildCaptureRequest(target, displayId, windowId, region, format, quality, grid, overlay);
            var shot = await _controller.CaptureAsync(request, cancellationToken).ConfigureAwait(false);
            return BuildCaptureResult(
                shot,
                new
                {
                    predictAction = "swipe",
                    from = new { x = fromX, y = fromY },
                    to = new { x = toX, y = toY }
                },
                "Predict swipe preview rendered.");
        }
        catch (Exception ex)
        {
            return Error("Failed to render swipe prediction.", ex);
        }
    }

    [McpServerTool(Name = "desktop.emergency_stop"), Description("Stops queued and future actions until reset.")]
    public CallToolResult EmergencyStop(string reason = "Emergency stop requested by operator.")
    {
        try
        {
            _controller.TriggerEmergencyStop(reason);
            return Ok(new { stopped = true, reason }, "Emergency stop activated.");
        }
        catch (Exception ex)
        {
            return Error("Failed to activate emergency stop.", ex);
        }
    }

    [McpServerTool(Name = "desktop.emergency_reset"), Description("Resets emergency stop and allows actions again.")]
    public CallToolResult EmergencyReset()
    {
        try
        {
            _controller.ResetEmergencyStop();
            return Ok(new { stopped = false }, "Emergency stop reset.");
        }
        catch (Exception ex)
        {
            return Error("Failed to reset emergency stop.", ex);
        }
    }

    private static CallToolResult Ok(object payload, string text)
    {
        return new CallToolResult
        {
            IsError = false,
            StructuredContent = JsonSerializer.SerializeToElement(payload),
            Content = [new TextContentBlock { Text = text }]
        };
    }

    private static CallToolResult Error(string message, Exception ex)
    {
        return new CallToolResult
        {
            IsError = true,
            StructuredContent = JsonSerializer.SerializeToElement(new
            {
                error = message,
                detail = ex.Message
            }),
            Content = [new TextContentBlock { Text = $"{message} {ex.Message}" }]
        };
    }

    private static MouseButton ParseMouseButton(string rawButton)
    {
        return rawButton.Trim().ToLowerInvariant() switch
        {
            "left" => MouseButton.Left,
            "right" => MouseButton.Right,
            "middle" => MouseButton.Middle,
            _ => throw new ArgumentException($"Unsupported mouse button: {rawButton}")
        };
    }

    private static CaptureTarget ParseTarget(string rawTarget)
    {
        return rawTarget.Trim().ToLowerInvariant() switch
        {
            "display" => CaptureTarget.Display,
            "window" => CaptureTarget.Window,
            "region" => CaptureTarget.Region,
            _ => throw new ArgumentException($"Unsupported capture target: {rawTarget}")
        };
    }

    private static CaptureFormat ParseFormat(string rawFormat)
    {
        return rawFormat.Trim().ToLowerInvariant() switch
        {
            "png" => CaptureFormat.Png,
            "jpeg" or "jpg" => CaptureFormat.Jpeg,
            _ => throw new ArgumentException($"Unsupported capture format: {rawFormat}")
        };
    }

    private static CaptureRequest BuildCaptureRequest(
        string target,
        string? displayId,
        string? windowId,
        CaptureRegionArgs? region,
        string format,
        int? quality,
        CaptureGridArgs? grid,
        PointerPreviewOverlay? overlay)
    {
        return new CaptureRequest(
            ParseTarget(target),
            DisplayId: displayId,
            WindowId: windowId,
            Region: region is null ? null : new ScreenRect(region.X, region.Y, region.Width, region.Height),
            Format: ParseFormat(format),
            Quality: quality,
            Grid: grid is null
                ? null
                : new GridOverlayOptions(grid.StepPx, grid.ShowLabels, grid.LineColor, grid.LineAlpha),
            PointerOverlay: overlay);
    }

    private static PointerPreviewOverlay BuildClickOverlay(ScreenPoint point, PointerOverlayStyleArgs? style)
    {
        style ??= new PointerOverlayStyleArgs();
        return new PointerPreviewOverlay(
            ClickPoint: point,
            SwipeFrom: null,
            SwipeTo: null,
            Color: style.Color,
            Alpha: style.Alpha,
            Thickness: style.Thickness,
            PointRadius: style.PointRadius,
            Label: style.Label ?? "click");
    }

    private static PointerPreviewOverlay BuildSwipeOverlay(ScreenPoint from, ScreenPoint to, PointerOverlayStyleArgs? style)
    {
        style ??= new PointerOverlayStyleArgs();
        return new PointerPreviewOverlay(
            ClickPoint: null,
            SwipeFrom: from,
            SwipeTo: to,
            Color: style.Color,
            Alpha: style.Alpha,
            Thickness: style.Thickness,
            PointRadius: style.PointRadius,
            Label: style.Label ?? "swipe");
    }

    private static CallToolResult BuildCaptureResult(
        CaptureResult shot,
        object? additionalStructuredContent,
        string message)
    {
        object payload = additionalStructuredContent is null
            ? new
            {
                width = shot.Width,
                height = shot.Height,
                bounds = shot.Bounds,
                cursorPos = shot.CursorPosition,
                target = shot.Target,
                sourceId = shot.SourceId
            }
            : new
            {
                width = shot.Width,
                height = shot.Height,
                bounds = shot.Bounds,
                cursorPos = shot.CursorPosition,
                target = shot.Target,
                sourceId = shot.SourceId,
                prediction = additionalStructuredContent
            };

        return new CallToolResult
        {
            IsError = false,
            StructuredContent = JsonSerializer.SerializeToElement(payload),
            Content =
            [
                ImageContentBlock.FromBytes(shot.ImageBytes, shot.MimeType),
                new TextContentBlock { Text = message }
            ]
        };
    }
}
