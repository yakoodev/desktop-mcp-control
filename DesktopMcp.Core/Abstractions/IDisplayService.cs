using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Abstractions;

public interface IDisplayService
{
    VirtualDesktopInfo GetVirtualDesktopInfo();
}
