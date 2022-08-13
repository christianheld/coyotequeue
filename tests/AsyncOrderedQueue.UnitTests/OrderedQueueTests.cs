namespace AsyncOrderedQueue.UnitTests;

public class OrderedQueueTests
{
    [Fact]
    public async Task DequeueSingleItem()
    {
        // Arrange
        var sut = new OrderedQueue<string>();
        var item = "Item";

        // Act
        var task = sut.DequeueAsync();

        sut.Enqueue(0, item);

        var actualItem = await task;

        // Assert
        Assert.Equal(item, actualItem);
    }

    [Fact]
    public async Task DequeuesInOrder()
    {
        // Arrange
        var sut = new OrderedQueue<string>();

        var item0 = "Zero";
        var item1 = "One";

        // Act
        var task = sut.DequeueAsync();

        sut.Enqueue(1, item1);
        sut.Enqueue(0, item0);

        var actualItem0 = await task;
        var actualItem1 = await sut.DequeueAsync();

        // Assert
        Assert.Equal(item0, actualItem0);
        Assert.Equal(item1, actualItem1);
    }
}
