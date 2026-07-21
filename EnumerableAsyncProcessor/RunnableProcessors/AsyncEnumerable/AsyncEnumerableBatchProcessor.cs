using EnumerableAsyncProcessor.Interfaces;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable;

public sealed class AsyncEnumerableBatchProcessor<TInput> : IAsyncEnumerableProcessor
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task> _taskSelector;
    private readonly int _batchSize;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private int _disposed;
    private Task? _executionTask;

    internal AsyncEnumerableBatchProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task> taskSelector,
        int batchSize,
        CancellationTokenSource cancellationTokenSource)
    {
        _items = items;
        _taskSelector = taskSelector;
        _batchSize = batchSize;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public Task ExecuteAsync()
    {
        var executionTask = ExecuteCoreAsync();
        _executionTask = executionTask;
        return executionTask;
    }

    private async Task ExecuteCoreAsync()
    {
        var cancellationToken = _cancellationTokenSource.Token;

        try
        {
            var batch = new List<TInput>(_batchSize);

            await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                batch.Add(item);

                if (batch.Count >= _batchSize)
                {
                    await ProcessBatch(batch).ConfigureAwait(false);
                    batch = new List<TInput>(_batchSize);
                }
            }

            // Process any remaining items in the final batch
            if (batch.Count > 0)
            {
                await ProcessBatch(batch).ConfigureAwait(false);
            }
        }
        finally
        {
            DisposeCancellationSource(cancelFirst: false);
        }
    }

    private async Task ProcessBatch(List<TInput> batch)
    {
        var tasks = batch.Select(item => _taskSelector(item)).ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(false);
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

    public async ValueTask DisposeAsync()
    {
        CancelForDisposal();

        // Mirror ProcessorLifecycle: give the in-flight run a bounded window to observe cancellation.
        if (_executionTask is { IsCompleted: false } executionTask)
        {
            try
            {
                await executionTask.WaitAsync(ProcessorLifecycle.DisposalTimeout).ConfigureAwait(false);
            }
            catch
            {
                // Cancellation, failure, or timeout of the in-flight run; disposal must not throw.
            }
        }

        DisposeCancellationSource(cancelFirst: false);
    }

    private void CancelForDisposal()
    {
        try
        {
            if (Volatile.Read(ref _disposed) == 0)
            {
                _cancellationTokenSource.Cancel();
            }
        }
        catch (ObjectDisposedException)
        {
            // The run completed and disposed the source concurrently - nothing left to cancel.
        }
    }
}
