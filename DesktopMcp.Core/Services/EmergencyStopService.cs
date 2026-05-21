using DesktopMcp.Core.Abstractions;

namespace DesktopMcp.Core.Services;

public sealed class EmergencyStopService : IEmergencyStopService, IDisposable
{
    private readonly object _sync = new();
    private CancellationTokenSource _cts = new();
    private string? _lastReason;

    public CancellationToken StopToken
    {
        get
        {
            lock (_sync)
            {
                return _cts.Token;
            }
        }
    }

    public bool IsStopped
    {
        get
        {
            lock (_sync)
            {
                return _cts.IsCancellationRequested;
            }
        }
    }

    public string? LastReason
    {
        get
        {
            lock (_sync)
            {
                return _lastReason;
            }
        }
    }

    public void Trigger(string reason)
    {
        lock (_sync)
        {
            _lastReason = string.IsNullOrWhiteSpace(reason) ? "Emergency stop requested." : reason.Trim();
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            _cts.Dispose();
            _cts = new CancellationTokenSource();
            _lastReason = null;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _cts.Dispose();
        }
    }
}
