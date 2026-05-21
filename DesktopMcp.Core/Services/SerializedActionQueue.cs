using DesktopMcp.Core.Abstractions;

namespace DesktopMcp.Core.Services;

public sealed class SerializedActionQueue : IActionQueue
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<T> EnqueueAsync<T>(
        Func<CancellationToken, Task<T>> action,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);
            return await action(cts.Token).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }
}
