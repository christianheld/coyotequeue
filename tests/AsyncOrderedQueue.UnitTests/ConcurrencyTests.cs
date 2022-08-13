namespace AsyncOrderedQueue.UnitTests;

public class ConcurrencyTests
{
    [Fact]
    [Microsoft.Coyote.SystematicTesting.Test]
    public static async Task TestConcurrentEnqueueDequeue()
    {
        // Arrange
        var queue = new OrderedQueue<int>();

        int count = 100;
        var tasks = new List<Task<int>>();

        // Act
        var dequeueTask = Task.Run(async () =>
        {
            for (int i = 0; i < count; i++)
            {
                await Task.Delay(Random.Shared.Next(0, 10));
                tasks.Add(queue.DequeueAsync());
            }
        });

        var addTask = Task.Run(async () =>
        {
            for (int i = count - 1; i >= 0; i--)
            {
                await Task.Delay(Random.Shared.Next(0, 10));
                queue.Enqueue(i, i);
            }
        });

        await Task.WhenAll(dequeueTask, addTask);

        var items = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(count, items.Length);
        for (int i = 0; i < count; i++)
        {
            Assert.Equal(i, items[i]);
        }
    }
}
