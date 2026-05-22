using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Abstractions;

public interface IDesktopCapabilityProvider
{
    DesktopCapabilities GetCapabilities();
}
