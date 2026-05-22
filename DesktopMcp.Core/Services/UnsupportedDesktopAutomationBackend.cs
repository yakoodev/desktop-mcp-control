using DesktopMcp.Core.Abstractions;
using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Services;

public sealed class UnsupportedDesktopAutomationBackend : IDesktopAutomationBackend
{
    private static readonly DesktopCapabilities Capabilities = new(
        Mouse: false,
        Keyboard: false,
        Capture: false,
        WindowList: false,
        Tray: false,
        GlobalHotkey: false);

    public DesktopCapabilities GetCapabilities() => Capabilities;

    public VirtualDesktopInfo GetVirtualDesktopInfo()
    {
        return new VirtualDesktopInfo(new ScreenRect(0, 0, 0, 0), []);
    }

    public IReadOnlyList<WindowInfo> ListWindows(WindowQuery query) => [];

    public bool TryGetWindowBounds(string windowId, out ScreenRect bounds)
    {
        bounds = default;
        return false;
    }

    public void MoveMouse(ScreenPoint point) => throw new NotSupportedException("Desktop automation is unavailable on this platform.");

    public void Click(ScreenPoint point, MouseButton button, int clickCount) => throw new NotSupportedException("Desktop automation is unavailable on this platform.");

    public void Scroll(ScreenPoint point, int deltaY, int deltaX) => throw new NotSupportedException("Desktop automation is unavailable on this platform.");

    public void TypeText(string text) => throw new NotSupportedException("Desktop automation is unavailable on this platform.");

    public void PressHotkey(IReadOnlyList<string> keys) => throw new NotSupportedException("Desktop automation is unavailable on this platform.");

    public Task DragDropAsync(ScreenPoint from, ScreenPoint to, MouseButton button, int durationMs, CancellationToken cancellationToken)
        => throw new NotSupportedException("Desktop automation is unavailable on this platform.");

    public ScreenPoint GetCursorPosition() => new(0, 0);

    public CaptureResult Capture(CaptureRequest request) => throw new NotSupportedException("Desktop automation is unavailable on this platform.");
}
