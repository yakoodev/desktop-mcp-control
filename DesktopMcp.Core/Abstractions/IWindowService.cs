using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Abstractions;

public interface IWindowService
{
    IReadOnlyList<WindowInfo> ListWindows(WindowQuery query);

    bool TryGetWindowBounds(string windowId, out ScreenRect bounds);
}
