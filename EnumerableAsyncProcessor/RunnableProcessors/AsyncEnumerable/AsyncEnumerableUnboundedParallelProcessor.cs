#if NET6_0_OR_GREATER
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable;

/// <summary>
/// A specialized parallel processor that starts ALL tasks immediately without any concurrency limits.
/// WARNING: Use with caution - this can overwhelm system resources with large async enumerables.
/// Unlike regular unbounded which materializes the collection first, this streams items but starts
/// processing each one immediately as it arrives.
/// </summary>
public class AsyncEnumerableUnboundedParallelProcessor<TInput> : IAsyncEnumerableProcessor
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    internal AsyncEnumerableUnboundedParallelProcessor(
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
        var tasks = new List<Task>();

        // Start a task for each item immediately as it arrives
        // No throttling or concurrency control
        await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // Start task immediately without waiting
            var task = _taskSelector(item);
            tasks.Add(task);
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
#endif