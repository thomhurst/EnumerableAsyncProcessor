using EnumerableAsyncProcessor.Interfaces;
using EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable.ResultProcessors;

namespace EnumerableAsyncProcessor.Builders;

public sealed class AsyncEnumerableActionAsyncProcessorBuilder<TInput, TOutput>
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task<TOutput>> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public AsyncEnumerableActionAsyncProcessorBuilder(
        IAsyncEnumerable<TInput> items, 
        Func<TInput, Task<TOutput>> taskSelector, 
        CancellationToken cancellationToken)
    {
        _items = items;
        _taskSelector = taskSelector;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    public AsyncEnumerableActionAsyncProcessorBuilder(
        IAsyncEnumerable<TInput> items,
        Func<TInput, CancellationToken, Task<TOutput>> taskSelector,
        CancellationToken cancellationToken)
    {
        _items = items;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _taskSelector = item => taskSelector(item, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// Process items in parallel, optionally limiting concurrency, and return results.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations, or null for unbounded concurrency.</param>
    /// <param name="scheduleOnThreadPool">For unbounded processing, schedules tasks on the thread pool when true.</param>
    /// <returns>An async processor configured for parallel execution.</returns>
    public IAsyncEnumerableProcessor<TOutput> ProcessInParallel(int? maxConcurrency = null, bool scheduleOnThreadPool = false)
    {
        return new ResultAsyncEnumerableParallelProcessor<TInput, TOutput>(
            _items, _taskSelector, maxConcurrency, scheduleOnThreadPool, _cancellationTokenSource);
    }
    
    /// <summary>
    /// Process items one at a time and return results in order.
    /// </summary>
    public IAsyncEnumerableProcessor<TOutput> ProcessOneAtATime()
    {
        return new ResultAsyncEnumerableOneAtATimeProcessor<TInput, TOutput>(
            _items, _taskSelector, _cancellationTokenSource);
    }
    
    /// <summary>
    /// Process items in batches and return results.
    /// </summary>
    /// <param name="batchSize">The size of each batch.</param>
    /// <returns>An async processor configured for batch execution.</returns>
    public IAsyncEnumerableProcessor<TOutput> ProcessInBatches(int batchSize)
    {
        return new ResultAsyncEnumerableBatchProcessor<TInput, TOutput>(
            _items, _taskSelector, batchSize, _cancellationTokenSource);
    }

}
