using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Abstractions;

public interface IInputAutomationService
{
    void MoveMouse(ScreenPoint point);

    void Click(ScreenPoint point, MouseButton button, int clickCount);

    void Scroll(ScreenPoint point, int deltaY, int deltaX);

    void TypeText(string text);

    void PressHotkey(IReadOnlyList<string> keys);

    Task DragDropAsync(
        ScreenPoint from,
        ScreenPoint to,
        MouseButton button,
        int durationMs,
        CancellationToken cancellationToken);

    ScreenPoint GetCursorPosition();
}
