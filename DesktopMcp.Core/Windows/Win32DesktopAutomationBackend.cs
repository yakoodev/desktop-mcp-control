using DesktopMcp.Core.Abstractions;
using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Windows;

public sealed class Win32DesktopAutomationBackend : IDesktopAutomationBackend
{
    private static readonly DesktopCapabilities Capabilities = new(
        Mouse: true,
        Keyboard: true,
        Capture: true,
        WindowList: true,
        Tray: true,
        GlobalHotkey: true);

    private readonly IDisplayService _displayService;
    private readonly IWindowService _windowService;
    private readonly IInputAutomationService _inputService;
    private readonly IScreenshotService _screenshotService;

    public Win32DesktopAutomationBackend(
        IDisplayService displayService,
        IWindowService windowService,
        IInputAutomationService inputService,
        IScreenshotService screenshotService)
    {
        _displayService = displayService;
        _windowService = windowService;
        _inputService = inputService;
        _screenshotService = screenshotService;
    }

    public DesktopCapabilities GetCapabilities() => Capabilities;

    public VirtualDesktopInfo GetVirtualDesktopInfo() => _displayService.GetVirtualDesktopInfo();

    public IReadOnlyList<WindowInfo> ListWindows(WindowQuery query) => _windowService.ListWindows(query);

    public bool TryGetWindowBounds(string windowId, out ScreenRect bounds) => _windowService.TryGetWindowBounds(windowId, out bounds);

    public void MoveMouse(ScreenPoint point) => _inputService.MoveMouse(point);

    public void Click(ScreenPoint point, MouseButton button, int clickCount) => _inputService.Click(point, button, clickCount);

    public void Scroll(ScreenPoint point, int deltaY, int deltaX) => _inputService.Scroll(point, deltaY, deltaX);

    public void TypeText(string text) => _inputService.TypeText(text);

    public void PressHotkey(IReadOnlyList<string> keys) => _inputService.PressHotkey(keys);

    public Task DragDropAsync(
        ScreenPoint from,
        ScreenPoint to,
        MouseButton button,
        int durationMs,
        CancellationToken cancellationToken)
    {
        return _inputService.DragDropAsync(from, to, button, durationMs, cancellationToken);
    }

    public ScreenPoint GetCursorPosition() => _inputService.GetCursorPosition();

    public CaptureResult Capture(CaptureRequest request) => _screenshotService.Capture(request);
}
