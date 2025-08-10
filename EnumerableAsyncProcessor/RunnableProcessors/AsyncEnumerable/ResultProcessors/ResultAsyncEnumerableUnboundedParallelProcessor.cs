#if NET6_0_OR_GREATER
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable.ResultProcessors;

/// <summary>
/// A specialized parallel processor that starts ALL tasks immediately without any concurrency limits
/// and returns results as they complete.
/// WARNING: Use with caution - this can overwhelm system resources with large async enumerables.
/// </summary>
public class ResultAsyncEnumerableUnboundedParallelProcessor<TInput, TOutput> : IAsyncEnumerableProcessor<TOutput>
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task<TOutput>> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    internal ResultAsyncEnumerableUnboundedParallelProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task<TOutput>> taskSelector,
        CancellationTokenSource cancellationTokenSource)
    {
        _items = items;
        _taskSelector = taskSelector;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public async IAsyncEnumerable<TOutput> ExecuteAsync()
    {
        var cancellationToken = _cancellationTokenSource.Token;
        var tasks = new List<Task<TOutput>>();

        // Start all tasks immediately
        await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var capturedItem = item;
            var task = Task.Run(async () =>
            {
                // Yield to ensure we don't block the thread if _taskSelector is synchronous
                await Task.Yield();
                return await _taskSelector(capturedItem).ConfigureAwait(false);
            }, cancellationToken);
            
            tasks.Add(task);
        }

        // Yield results as tasks complete
        while (tasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(tasks).ConfigureAwait(false);
            tasks.Remove(completedTask);
            yield return await completedTask.ConfigureAwait(false);
        }
    }
}
#endif