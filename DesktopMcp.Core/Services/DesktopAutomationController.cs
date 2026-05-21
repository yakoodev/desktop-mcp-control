using DesktopMcp.Core.Abstractions;
using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Services;

public sealed class DesktopAutomationController : IDesktopAutomationController
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly IDisplayService _displayService;
    private readonly IWindowService _windowService;
    private readonly IInputAutomationService _inputService;
    private readonly IScreenshotService _screenshotService;
    private readonly IActionQueue _queue;
    private readonly IEmergencyStopService _emergencyStop;

    public DesktopAutomationController(
        IDisplayService displayService,
        IWindowService windowService,
        IInputAutomationService inputService,
        IScreenshotService screenshotService,
        IActionQueue queue,
        IEmergencyStopService emergencyStop)
    {
        _displayService = displayService;
        _windowService = windowService;
        _inputService = inputService;
        _screenshotService = screenshotService;
        _queue = queue;
        _emergencyStop = emergencyStop;
    }

    public Task<VirtualDesktopInfo> GetDisplaysAsync(CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(
            _ => Task.FromResult(_displayService.GetVirtualDesktopInfo()),
            DefaultTimeout,
            cancellationToken);
    }

    public Task<IReadOnlyList<WindowInfo>> ListWindowsAsync(WindowQuery query, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(
            _ => Task.FromResult(_windowService.ListWindows(query)),
            DefaultTimeout,
            cancellationToken);
    }

    public Task<ScreenPoint> MouseMoveAsync(ScreenPoint point, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(
            _ =>
            {
                _inputService.MoveMouse(point);
                return Task.FromResult(_inputService.GetCursorPosition());
            },
            DefaultTimeout,
            cancellationToken);
    }

    public Task<ScreenPoint> MouseClickAsync(
        ScreenPoint point,
        MouseButton button,
        int clickCount,
        CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(
            _ =>
            {
                _inputService.Click(point, button, clickCount);
                return Task.FromResult(_inputService.GetCursorPosition());
            },
            DefaultTimeout,
            cancellationToken);
    }

    public Task<ScreenPoint> MouseScrollAsync(
        ScreenPoint point,
        int deltaY,
        int deltaX,
        CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(
            _ =>
            {
                _inputService.Scroll(point, deltaY, deltaX);
                return Task.FromResult(_inputService.GetCursorPosition());
            },
            DefaultTimeout,
            cancellationToken);
    }

    public Task KeyboardTypeAsync(string text, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(
            _ =>
            {
                _inputService.TypeText(text);
                return Task.FromResult(true);
            },
            DefaultTimeout,
            cancellationToken);
    }

    public Task KeyboardHotkeyAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(
            _ =>
            {
                _inputService.PressHotkey(keys);
                return Task.FromResult(true);
            },
            DefaultTimeout,
            cancellationToken);
    }

    public Task DragDropAsync(
        ScreenPoint from,
        ScreenPoint to,
        MouseButton button,
        int durationMs,
        CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(
            async token =>
            {
                await _inputService.DragDropAsync(from, to, button, durationMs, token).ConfigureAwait(false);
                return true;
            },
            TimeSpan.FromMilliseconds(Math.Max(1000, durationMs + 5000)),
            cancellationToken);
    }

    public Task<CaptureResult> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(
            _ => Task.FromResult(_screenshotService.Capture(request)),
            TimeSpan.FromSeconds(20),
            cancellationToken);
    }

    public void TriggerEmergencyStop(string reason)
    {
        _emergencyStop.Trigger(reason);
    }

    public void ResetEmergencyStop()
    {
        _emergencyStop.Reset();
    }

    private async Task<T> EnqueueAsync<T>(
        Func<CancellationToken, Task<T>> action,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _emergencyStop.StopToken);
        return await _queue.EnqueueAsync(action, timeout, linked.Token).ConfigureAwait(false);
    }
}
