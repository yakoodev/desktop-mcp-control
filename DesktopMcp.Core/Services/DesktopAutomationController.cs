using DesktopMcp.Core.Abstractions;
using DesktopMcp.Core.Exceptions;
using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Services;

public sealed class DesktopAutomationController : IDesktopAutomationController
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly IDesktopAutomationBackend _backend;
    private readonly IActionQueue _queue;
    private readonly IEmergencyStopService _emergencyStop;

    public DesktopAutomationController(
        IDesktopAutomationBackend backend,
        IActionQueue queue,
        IEmergencyStopService emergencyStop)
    {
        _backend = backend;
        _queue = queue;
        _emergencyStop = emergencyStop;
    }

    public Task<DesktopCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(
            _ => Task.FromResult(_backend.GetCapabilities()),
            DefaultTimeout,
            cancellationToken);
    }

    public Task<VirtualDesktopInfo> GetDisplaysAsync(CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(
            _ => Task.FromResult(_backend.GetVirtualDesktopInfo()),
            DefaultTimeout,
            cancellationToken);
    }

    public Task<IReadOnlyList<WindowInfo>> ListWindowsAsync(WindowQuery query, CancellationToken cancellationToken = default)
    {
        EnsureCapability(DesktopCapability.WindowList);
        return EnqueueAsync(
            _ => Task.FromResult(_backend.ListWindows(query)),
            DefaultTimeout,
            cancellationToken);
    }

    public Task<ScreenPoint> MouseMoveAsync(ScreenPoint point, CancellationToken cancellationToken = default)
    {
        EnsureCapability(DesktopCapability.Mouse);
        return EnqueueAsync(
            _ =>
            {
                _backend.MoveMouse(point);
                return Task.FromResult(_backend.GetCursorPosition());
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
        EnsureCapability(DesktopCapability.Mouse);
        return EnqueueAsync(
            _ =>
            {
                _backend.Click(point, button, clickCount);
                return Task.FromResult(_backend.GetCursorPosition());
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
        EnsureCapability(DesktopCapability.Mouse);
        return EnqueueAsync(
            _ =>
            {
                _backend.Scroll(point, deltaY, deltaX);
                return Task.FromResult(_backend.GetCursorPosition());
            },
            DefaultTimeout,
            cancellationToken);
    }

    public Task KeyboardTypeAsync(string text, CancellationToken cancellationToken = default)
    {
        EnsureCapability(DesktopCapability.Keyboard);
        return EnqueueAsync(
            _ =>
            {
                _backend.TypeText(text);
                return Task.FromResult(true);
            },
            DefaultTimeout,
            cancellationToken);
    }

    public Task KeyboardHotkeyAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken = default)
    {
        EnsureCapability(DesktopCapability.Keyboard);
        return EnqueueAsync(
            _ =>
            {
                _backend.PressHotkey(keys);
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
        EnsureCapability(DesktopCapability.Mouse);
        return EnqueueAsync(
            async token =>
            {
                await _backend.DragDropAsync(from, to, button, durationMs, token).ConfigureAwait(false);
                return true;
            },
            TimeSpan.FromMilliseconds(Math.Max(1000, durationMs + 5000)),
            cancellationToken);
    }

    public Task<CaptureResult> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCapability(DesktopCapability.Capture);
        return EnqueueAsync(
            _ => Task.FromResult(_backend.Capture(request)),
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

    private void EnsureCapability(DesktopCapability capability)
    {
        if (_backend.GetCapabilities().Has(capability))
        {
            return;
        }

        throw new CapabilityUnavailableException(capability);
    }
}
