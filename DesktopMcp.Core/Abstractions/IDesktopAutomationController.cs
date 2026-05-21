using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Abstractions;

public interface IDesktopAutomationController
{
    Task<VirtualDesktopInfo> GetDisplaysAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WindowInfo>> ListWindowsAsync(WindowQuery query, CancellationToken cancellationToken = default);

    Task<ScreenPoint> MouseMoveAsync(ScreenPoint point, CancellationToken cancellationToken = default);

    Task<ScreenPoint> MouseClickAsync(
        ScreenPoint point,
        MouseButton button,
        int clickCount,
        CancellationToken cancellationToken = default);

    Task<ScreenPoint> MouseScrollAsync(
        ScreenPoint point,
        int deltaY,
        int deltaX,
        CancellationToken cancellationToken = default);

    Task KeyboardTypeAsync(string text, CancellationToken cancellationToken = default);

    Task KeyboardHotkeyAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken = default);

    Task DragDropAsync(
        ScreenPoint from,
        ScreenPoint to,
        MouseButton button,
        int durationMs,
        CancellationToken cancellationToken = default);

    Task<CaptureResult> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default);

    void TriggerEmergencyStop(string reason);

    void ResetEmergencyStop();
}
