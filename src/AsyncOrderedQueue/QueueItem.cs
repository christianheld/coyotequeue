namespace AsyncOrderedQueue;

internal record class QueueItem<T>(int SequenceNumber, T Value);

internal static class QueueItem
{
    public static QueueItem<T> Create<T>(int sequenceNumber, T value) =>
        new QueueItem<T>(sequenceNumber, value);
}
