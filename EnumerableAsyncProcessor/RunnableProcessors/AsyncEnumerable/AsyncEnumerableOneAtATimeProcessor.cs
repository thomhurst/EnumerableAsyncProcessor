using EnumerableAsyncProcessor.Interfaces;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable;

/// <summary>
/// Sequential processor that processes items one at a time from an IAsyncEnumerable.
/// </summary>
public sealed class AsyncEnumerableOneAtATimeProcessor<TInput> : IAsyncEnumerableProcessor
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private int _disposed;

    internal AsyncEnumerableOneAtATimeProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task> taskSelector,
        CancellationTokenSource cancellationTokenSource)
    {
        _items = items;
        _taskSelector = taskSelector;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public async Task ExecuteAsync()
    {
        var cancellationToken = _cancellationTokenSource.Token;

        try
        {
            await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await _taskSelector(item).ConfigureAwait(false);
            }
        }
        finally
        {
            DisposeCancellationSource(cancelFirst: false);
        }
    }

    public void Dispose()
    {
        DisposeCancellationSource(cancelFirst: true);
    }

    // Explicit disposal cancels in-flight work first; the completion path has nothing left to cancel.
    private void DisposeCancellationSource(bool cancelFirst)
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        if (cancelFirst)
        {
            _cancellationTokenSource.Cancel();
        }

        _cancellationTokenSource.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
