using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.Interfaces;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable.ResultProcessors;

public sealed class ResultAsyncEnumerableParallelProcessor<TInput, TOutput> : IAsyncEnumerableProcessor<TOutput>
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task<TOutput>> _taskSelector;
    private readonly int? _maxConcurrency;
    private readonly bool _scheduleOnThreadPool;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private int _disposed;

    internal ResultAsyncEnumerableParallelProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task<TOutput>> taskSelector,
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

    public async IAsyncEnumerable<TOutput> ExecuteAsync()
    {
        var cancellationToken = _cancellationTokenSource.Token;

        try
        {
            if (_maxConcurrency.HasValue)
            {
                Func<TInput, Task<TOutput>> taskSelector = _scheduleOnThreadPool
                    ? item => Task.Run(() => _taskSelector(item), cancellationToken)
                    : _taskSelector;

                await foreach (var result in AsyncEnumerableWorkerPool.ProcessResultsAsync(
                                   _items,
                                   taskSelector,
                                   _maxConcurrency.Value,
                                   cancellationToken).ConfigureAwait(false))
                {
                    yield return result;
                }

                yield break;
            }

            var tasks = new List<Task<TOutput>>();

            // Unbounded parallel processing
            try
            {
                await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    var capturedItem = item;

                    Task<TOutput> task;
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

                // Yield all results as they complete
                await foreach (var result in tasks.ToIAsyncEnumerable(cancellationToken).ConfigureAwait(false))
                {
                    yield return result;
                }
            }
            finally
            {
                if (tasks.Count > 0)
                {
                    try
                    {
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Preserve the exception already propagating from enumeration or result consumption.
                    }
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

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
