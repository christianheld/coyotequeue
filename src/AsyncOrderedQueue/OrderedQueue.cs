namespace AsyncOrderedQueue;

public sealed class OrderedQueue<T>
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<int, T> _storage = new();
    private readonly Dictionary<int, TaskCompletionSource<T>> _handlers = new();

    private int _exceptedSequenceNumber;

    public OrderedQueue(int firstSequenceNumber = 0)
    {
        _exceptedSequenceNumber = firstSequenceNumber;
    }

    public void Enqueue(int sequenceNumber, T item)
    {
        lock (_syncRoot)
        {
            if (_handlers.TryGetValue(sequenceNumber, out var handler))
            {
                _handlers.Remove(sequenceNumber);
                handler.SetResult(item);
            }
            else
            {
                _storage.Add(sequenceNumber, item);
            }
        }
    }

    public async Task<T> DequeueAsync(CancellationToken cancellationToken = default)
        => await DequeueAsync(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);

    public async Task<T> DequeueAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            if (_storage.TryGetValue(_exceptedSequenceNumber, out var item))
            {
                Interlocked.Increment(ref _exceptedSequenceNumber);
                return item;
            }
        }

        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var timeoutTask = Task.Delay(timeout, cancellationTokenSource.Token);

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.Register(() => tcs.SetCanceled());

        lock (_syncRoot)
        {
            _handlers.Add(_exceptedSequenceNumber, tcs);
            Interlocked.Increment(ref _exceptedSequenceNumber);
        }

        var response = await Task.WhenAny(tcs.Task, timeoutTask).ConfigureAwait(false);
        if (response == timeoutTask)
        {
            throw new TimeoutException();
        }

        return await tcs.Task.ConfigureAwait(false);
    }
}
