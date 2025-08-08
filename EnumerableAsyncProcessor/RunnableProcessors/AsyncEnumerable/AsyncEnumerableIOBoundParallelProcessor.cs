#if NET6_0_OR_GREATER
using System.Threading.Channels;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable;

/// <summary>
/// Optimized parallel processor for I/O-bound operations on IAsyncEnumerable.
/// Uses higher concurrency levels and avoids Task.Run overhead.
/// </summary>
public class AsyncEnumerableIOBoundParallelProcessor<TInput> : IAsyncEnumerableProcessor
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task> _taskSelector;
    private readonly int _maxConcurrency;
    private readonly CancellationTokenSource _cancellationTokenSource;

    internal AsyncEnumerableIOBoundParallelProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task> taskSelector,
        int maxConcurrency,
        CancellationTokenSource cancellationTokenSource)
    {
        _items = items;
        _taskSelector = taskSelector;
        _maxConcurrency = maxConcurrency;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public async Task ExecuteAsync()
    {
        var cancellationToken = _cancellationTokenSource.Token;
        var semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
        var tasks = new List<Task>();

        try
        {
            await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                // Start task without Task.Run for I/O-bound operations
                var task = ProcessItemAsync(item, semaphore, cancellationToken);
                tasks.Add(task);

                // Clean up completed tasks periodically to avoid memory growth
                if (tasks.Count > _maxConcurrency * 2)
                {
                    tasks.RemoveAll(t => t.IsCompleted);
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Dispose();
        }
    }

    private async Task ProcessItemAsync(TInput item, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        try
        {
            await _taskSelector(item).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
#endif