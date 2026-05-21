using DesktopMcp.Core.Services;

namespace DesktopMcp.Tests;

public class SerializedActionQueueTests
{
    [Fact]
    public async Task Queue_ExecutesActionsSequentially()
    {
        var queue = new SerializedActionQueue();
        var order = new List<int>();

        var task1 = queue.EnqueueAsync(
            async _ =>
            {
                await Task.Delay(100);
                order.Add(1);
                return 1;
            },
            TimeSpan.FromSeconds(2));

        var task2 = queue.EnqueueAsync(
            async _ =>
            {
                order.Add(2);
                await Task.Delay(10);
                return 2;
            },
            TimeSpan.FromSeconds(2));

        await Task.WhenAll(task1, task2);

        Assert.Equal([1, 2], order);
    }

    [Fact]
    public async Task Queue_ThrowsOnTimeout()
    {
        var queue = new SerializedActionQueue();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => queue.EnqueueAsync(
                async token =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), token);
                    return 42;
                },
                TimeSpan.FromMilliseconds(50)));
    }
}
