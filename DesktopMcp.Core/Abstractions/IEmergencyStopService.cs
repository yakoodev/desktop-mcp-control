namespace DesktopMcp.Core.Abstractions;

public interface IEmergencyStopService
{
    CancellationToken StopToken { get; }

    bool IsStopped { get; }

    string? LastReason { get; }

    void Trigger(string reason);

    void Reset();
}
