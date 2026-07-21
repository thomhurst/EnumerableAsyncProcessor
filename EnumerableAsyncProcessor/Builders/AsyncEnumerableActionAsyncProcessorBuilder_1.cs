using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable;

namespace EnumerableAsyncProcessor.Builders;

public class AsyncEnumerableActionAsyncProcessorBuilder<TInput>
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public AsyncEnumerableActionAsyncProcessorBuilder(
        IAsyncEnumerable<TInput> items, 
        Func<TInput, Task> taskSelector, 
        CancellationToken cancellationToken)
    {
        _items = items;
        _taskSelector = taskSelector;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    public AsyncEnumerableActionAsyncProcessorBuilder(
        IAsyncEnumerable<TInput> items,
        Func<TInput, CancellationToken, Task> taskSelector,
        CancellationToken cancellationToken)
    {
        _items = items;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _taskSelector = item => taskSelector(item, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// Process items in parallel, optionally limiting concurrency.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations, or null for unbounded concurrency.</param>
    /// <param name="scheduleOnThreadPool">For unbounded processing, schedules tasks on the thread pool when true.</param>
    /// <returns>An async processor configured for parallel execution.</returns>
    public IAsyncEnumerableProcessor ProcessInParallel(int? maxConcurrency = null, bool scheduleOnThreadPool = false)
    {
        return new AsyncEnumerableParallelProcessor<TInput>(
            _items, _taskSelector, maxConcurrency, scheduleOnThreadPool, _cancellationTokenSource);
    }
    
    /// <summary>
    /// Process items one at a time (sequential processing).
    /// </summary>
    public IAsyncEnumerableProcessor ProcessOneAtATime()
    {
        return new AsyncEnumerableOneAtATimeProcessor<TInput>(
            _items, _taskSelector, _cancellationTokenSource);
    }
    
    /// <summary>
    /// Process items in batches.
    /// </summary>
    /// <param name="batchSize">The size of each batch.</param>
    /// <returns>An async processor configured for batch execution.</returns>
    public IAsyncEnumerableProcessor ProcessInBatches(int batchSize)
    {
        return new AsyncEnumerableBatchProcessor<TInput>(
            _items, _taskSelector, batchSize, _cancellationTokenSource);
    }

}
