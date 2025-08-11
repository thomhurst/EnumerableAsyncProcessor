#if NET6_0_OR_GREATER
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

    /// <summary>
    /// Process items in parallel without concurrency limits.
    /// </summary>
    /// <returns>An async processor configured for parallel execution.</returns>
    public IAsyncEnumerableProcessor ProcessInParallel()
    {
        return ProcessInParallel(null, false);
    }
    
    /// <summary>
    /// Process items in parallel without concurrency limits.
    /// </summary>
    /// <param name="scheduleOnThreadPool">If true, schedules tasks on thread pool to prevent blocking. Default is false for maximum performance.</param>
    /// <returns>An async processor configured for parallel execution.</returns>
    public IAsyncEnumerableProcessor ProcessInParallel(bool scheduleOnThreadPool)
    {
        return ProcessInParallel(null, scheduleOnThreadPool);
    }
    
    /// <summary>
    /// Process items in parallel with specified concurrency limit.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations.</param>
    /// <returns>An async processor configured for parallel execution.</returns>
    public IAsyncEnumerableProcessor ProcessInParallel(int maxConcurrency)
    {
        return ProcessInParallel((int?)maxConcurrency, false);
    }
    
    /// <summary>
    /// Process items in parallel with specified concurrency limit.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations.</param>
    /// <returns>An async processor configured for parallel execution.</returns>
    public IAsyncEnumerableProcessor ProcessInParallel(int? maxConcurrency)
    {
        return ProcessInParallel(maxConcurrency, false);
    }
    
    /// <summary>
    /// Process items in parallel with specified concurrency limit.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations.</param>
    /// <param name="scheduleOnThreadPool">If true, schedules tasks on thread pool to prevent blocking.</param>
    /// <returns>An async processor configured for parallel execution.</returns>
    public IAsyncEnumerableProcessor ProcessInParallel(int? maxConcurrency, bool scheduleOnThreadPool)
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
#endif