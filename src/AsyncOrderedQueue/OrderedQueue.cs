namespace AsyncOrderedQueue;

/// <summary>
/// Unbounded storage that will dequeue items in order of expected sequence numbers.
/// </summary>
/// <typeparam name="T">The typoe of elements to be stored.</typeparam>
public sealed class OrderedQueue<T>
{
    private readonly Dictionary<int, TaskCompletionSource<T>> _handlers = new();
    private readonly Dictionary<int, T> _buffer = new();

    // Note: Our data structures are not thread safe => we need locks
    private readonly object _syncRoot = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedQueue{T}"/> class.
    /// </summary>
    /// <param name="firstSequenceNumber">The first sequence number.</param>
    public OrderedQueue(int firstSequenceNumber = 0)
    {
        ExceptedSequenceNumber = firstSequenceNumber;
    }

    /// <summary>
    /// Gets the count of enqueued items
    /// </summary>
    public int BufferedItemCount
    {
        get
        {
            lock (_syncRoot)
            {
                return _buffer.Count;
            }
        }
    }

    /// <summary>
    /// Gets the next expected sequence number.
    /// </summary>
    public int ExceptedSequenceNumber { get; private set; }

    /// <summary>
    /// Dequeues the next item from the queue.
    /// </summary>
    /// <returns>The item with the next expected sequence number.</returns>
    public Task<T> DequeueAsync()
    {
        lock (_syncRoot)
        {
            // Our expected item is already buffered
            if (_buffer.TryGetValue(ExceptedSequenceNumber, out var item))
            {
                _buffer.Remove(ExceptedSequenceNumber);
                ExceptedSequenceNumber++;
                return Task.FromResult(item);
            }

            // Our expected item has not arrived yet.
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            _handlers.Add(ExceptedSequenceNumber, tcs);
            ExceptedSequenceNumber++;

            return tcs.Task;
        }
    }

    /// <summary>
    /// Adds an item to the queue.
    /// </summary>
    /// <param name="sequenceNumber">The items sequence number.</param>
    /// <param name="item">The item.</param>
    public void Enqueue(int sequenceNumber, T item)
    {
        lock (_syncRoot)
        {
            // Someone is already waiting for this item. No need to buffer
            if (_handlers.TryGetValue(sequenceNumber, out var handler))
            {
                _handlers.Remove(sequenceNumber);
                handler.SetResult(item);
                return;
            }

            // Keep item for future dequeue operations.
            _buffer.Add(sequenceNumber, item);
        }
    }
}
