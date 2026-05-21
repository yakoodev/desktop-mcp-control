namespace DesktopMcp.Core.Abstractions;

public interface IActionQueue
{
    Task<T> EnqueueAsync<T>(
        Func<CancellationToken, Task<T>> action,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
