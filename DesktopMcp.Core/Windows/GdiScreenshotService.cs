using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using DesktopMcp.Core.Abstractions;
using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Windows;

[SupportedOSPlatform("windows")]
public sealed class GdiScreenshotService : IScreenshotService
{
    private readonly IDisplayService _displayService;
    private readonly IWindowService _windowService;
    private readonly IInputAutomationService _inputService;

    public GdiScreenshotService(
        IDisplayService displayService,
        IWindowService windowService,
        IInputAutomationService inputService)
    {
        _displayService = displayService;
        _windowService = windowService;
        _inputService = inputService;
    }

    public CaptureResult Capture(CaptureRequest request)
    {
        var (bounds, sourceId) = ResolveBounds(request);
        if (bounds.IsEmpty)
        {
            throw new ArgumentException("Resolved capture bounds are empty.");
        }

        using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, new Size(bounds.Width, bounds.Height), CopyPixelOperation.SourceCopy);

        if (request.Grid is { } grid)
        {
            DrawGrid(graphics, bounds, grid);
        }

        if (request.PointerOverlay is { } pointerOverlay)
        {
            DrawPointerOverlay(graphics, bounds, pointerOverlay);
        }

        using var ms = new MemoryStream();
        if (request.Format == CaptureFormat.Jpeg)
        {
            SaveAsJpeg(bitmap, ms, request.Quality ?? 90);
        }
        else
        {
            bitmap.Save(ms, ImageFormat.Png);
        }

        var cursor = _inputService.GetCursorPosition();
        return new CaptureResult(
            ms.ToArray(),
            request.Format == CaptureFormat.Jpeg ? "image/jpeg" : "image/png",
            bounds.Width,
            bounds.Height,
            bounds,
            cursor,
            request.Target.ToString().ToLowerInvariant(),
            sourceId);
    }

    private (ScreenRect bounds, string? sourceId) ResolveBounds(CaptureRequest request)
    {
        return request.Target switch
        {
            CaptureTarget.Display => ResolveDisplayBounds(request.DisplayId),
            CaptureTarget.Window => ResolveWindowBounds(request.WindowId),
            CaptureTarget.Region => ResolveRegionBounds(request.Region),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Target))
        };
    }

    private (ScreenRect bounds, string? sourceId) ResolveDisplayBounds(string? displayId)
    {
        var desktop = _displayService.GetVirtualDesktopInfo();
        var display = string.IsNullOrWhiteSpace(displayId)
            ? desktop.Displays.FirstOrDefault(d => d.IsPrimary) ?? desktop.Displays.FirstOrDefault()
            : desktop.Displays.FirstOrDefault(d => d.DisplayId.Equals(displayId, StringComparison.OrdinalIgnoreCase));

        if (display is null)
        {
            throw new ArgumentException($"Display not found: {displayId}");
        }

        return (display.Bounds, display.DisplayId);
    }

    private (ScreenRect bounds, string? sourceId) ResolveWindowBounds(string? windowId)
    {
        if (string.IsNullOrWhiteSpace(windowId))
        {
            throw new ArgumentException("windowId is required for window capture.");
        }

        if (!_windowService.TryGetWindowBounds(windowId, out var bounds))
        {
            throw new ArgumentException($"Window not found: {windowId}");
        }

        return (bounds, windowId);
    }

    private static (ScreenRect bounds, string? sourceId) ResolveRegionBounds(ScreenRect? region)
    {
        if (region is null || region.Value.IsEmpty)
        {
            throw new ArgumentException("region is required for region capture.");
        }

        return (region.Value, null);
    }

    private static void DrawGrid(Graphics graphics, ScreenRect bounds, GridOverlayOptions options)
    {
        var step = Math.Max(8, options.StepPx);
        var alpha = (int)Math.Round(Math.Clamp(options.LineAlpha, 0f, 1f) * 255f);
        var color = ParseColor(options.LineColor, alpha);

        using var pen = new Pen(color, 1f) { DashStyle = DashStyle.Dot };
        using var font = new Font("Consolas", 9f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var labelBrush = new SolidBrush(Color.FromArgb(Math.Min(255, alpha + 80), color));
        graphics.SmoothingMode = SmoothingMode.HighSpeed;

        for (var x = 0; x < bounds.Width; x += step)
        {
            graphics.DrawLine(pen, x, 0, x, bounds.Height);
            if (options.ShowLabels)
            {
                graphics.DrawString($"{bounds.X + x}", font, labelBrush, x + 2, 2);
            }
        }

        for (var y = 0; y < bounds.Height; y += step)
        {
            graphics.DrawLine(pen, 0, y, bounds.Width, y);
            if (options.ShowLabels)
            {
                graphics.DrawString($"{bounds.Y + y}", font, labelBrush, 2, y + 2);
            }
        }
    }

    private static void DrawPointerOverlay(Graphics graphics, ScreenRect bounds, PointerPreviewOverlay overlay)
    {
        var alpha = (int)Math.Round(Math.Clamp(overlay.Alpha, 0f, 1f) * 255f);
        var color = ParseColor(overlay.Color, alpha);
        var radius = Math.Max(3, overlay.PointRadius);
        var thickness = Math.Max(1, overlay.Thickness);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        if (overlay.SwipeFrom is { } swipeFrom &&
            overlay.SwipeTo is { } swipeTo &&
            (swipeFrom.X != swipeTo.X || swipeFrom.Y != swipeTo.Y))
        {
            var from = ToLocal(swipeFrom, bounds);
            var to = ToLocal(swipeTo, bounds);

            using var pen = new Pen(color, thickness)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            using var arrowCap = new AdjustableArrowCap(5f + thickness, 9f + thickness * 1.5f, true);
            pen.CustomEndCap = arrowCap;
            graphics.DrawLine(pen, from, to);

            using var startBrush = new SolidBrush(Color.FromArgb(Math.Min(255, alpha), color));
            using var endBrush = new SolidBrush(Color.FromArgb(Math.Min(255, alpha), color));
            graphics.FillEllipse(startBrush, from.X - radius, from.Y - radius, radius * 2, radius * 2);
            graphics.FillEllipse(endBrush, to.X - radius, to.Y - radius, radius * 2, radius * 2);

            DrawOverlayLabel(
                graphics,
                overlay.Label ?? "swipe",
                (from.X + to.X) * 0.5f + 10f,
                (from.Y + to.Y) * 0.5f - 22f,
                color);
        }

        if (overlay.ClickPoint is { } clickPoint)
        {
            var click = ToLocal(clickPoint, bounds);
            using var ringPen = new Pen(color, thickness);
            graphics.DrawEllipse(ringPen, click.X - radius, click.Y - radius, radius * 2, radius * 2);

            using var dotBrush = new SolidBrush(Color.FromArgb(Math.Min(255, alpha), color));
            var dotRadius = Math.Max(2, radius / 3f);
            graphics.FillEllipse(dotBrush, click.X - dotRadius, click.Y - dotRadius, dotRadius * 2, dotRadius * 2);

            using var crossPen = new Pen(Color.FromArgb(Math.Min(255, alpha), color), Math.Max(1, thickness - 1));
            var cross = radius + 5;
            graphics.DrawLine(crossPen, click.X - cross, click.Y, click.X + cross, click.Y);
            graphics.DrawLine(crossPen, click.X, click.Y - cross, click.X, click.Y + cross);

            DrawOverlayLabel(
                graphics,
                overlay.Label ?? "click",
                click.X + radius + 8f,
                click.Y - radius - 18f,
                color);
        }
    }

    private static void SaveAsJpeg(Bitmap bitmap, Stream output, int quality)
    {
        quality = Math.Clamp(quality, 1, 100);
        var codec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
        if (codec is null)
        {
            bitmap.Save(output, ImageFormat.Jpeg);
            return;
        }

        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
        bitmap.Save(output, codec, parameters);
    }

    private static Color ParseColor(string rawColor, int alpha)
    {
        try
        {
            var baseColor = ColorTranslator.FromHtml(rawColor);
            return Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
        }
        catch
        {
            return Color.FromArgb(alpha, 0, 255, 136);
        }
    }

    private static PointF ToLocal(ScreenPoint point, ScreenRect bounds)
    {
        return new PointF(point.X - bounds.X, point.Y - bounds.Y);
    }

    private static void DrawOverlayLabel(Graphics graphics, string text, float x, float y, Color color)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        using var font = new Font("Consolas", 10f, FontStyle.Bold, GraphicsUnit.Pixel);
        var labelColor = Color.FromArgb(235, 16, 22, 40);
        var textColor = Color.FromArgb(255, color);
        var size = graphics.MeasureString(text, font);
        var rect = new RectangleF(x, y, size.Width + 10f, size.Height + 6f);

        using var bg = new SolidBrush(labelColor);
        using var fg = new SolidBrush(textColor);
        graphics.FillRectangle(bg, rect);
        graphics.DrawString(text, font, fg, x + 5f, y + 2f);
    }
}
