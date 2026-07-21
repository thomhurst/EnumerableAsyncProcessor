using EnumerableAsyncProcessor.Interfaces;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable;

public sealed class AsyncEnumerableParallelProcessor<TInput> : IAsyncEnumerableProcessor
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task> _taskSelector;
    private readonly int? _maxConcurrency;
    private readonly bool _scheduleOnThreadPool;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private int _disposed;
    private Task? _executionTask;

    internal AsyncEnumerableParallelProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task> taskSelector,
        int? maxConcurrency,
        bool scheduleOnThreadPool,
        CancellationTokenSource cancellationTokenSource)
    {
        _items = items;
        _taskSelector = taskSelector;
        _maxConcurrency = maxConcurrency;
        _scheduleOnThreadPool = scheduleOnThreadPool;
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
            if (_maxConcurrency.HasValue)
            {
                Func<TInput, Task> taskSelector = _scheduleOnThreadPool
                    ? item => Task.Run(() => _taskSelector(item), cancellationToken)
                    : _taskSelector;

                await AsyncEnumerableWorkerPool.ProcessAsync(
                    _items,
                    taskSelector,
                    _maxConcurrency.Value,
                    cancellationToken).ConfigureAwait(false);

                return;
            }

            // Unbounded parallel processing
            var tasks = new List<Task>();

            try
            {
                await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    var capturedItem = item;

                    Task task;
                    if (_scheduleOnThreadPool)
                    {
                        task = Task.Run(() => _taskSelector(capturedItem), cancellationToken);
                    }
                    else
                    {
                        task = _taskSelector(capturedItem);
                    }

                    tasks.Add(task);
                }
            }
            finally
            {
                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
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
