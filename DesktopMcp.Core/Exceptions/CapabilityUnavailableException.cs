using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Exceptions;

public sealed class CapabilityUnavailableException : InvalidOperationException
{
    public CapabilityUnavailableException(DesktopCapability capability, string? message = null)
        : base(message ?? $"Capability '{capability}' is unavailable on this platform.")
    {
        Capability = capability;
    }

    public DesktopCapability Capability { get; }
}
