using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable.ResultProcessors;

namespace EnumerableAsyncProcessor.Builders;

public class AsyncEnumerableActionAsyncProcessorBuilder<TInput, TOutput>
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

    /// <summary>
    /// Process items in parallel without concurrency limits and return results.
    /// </summary>
    /// <returns>An async processor configured for parallel execution.</returns>
    public IAsyncEnumerableProcessor<TOutput> ProcessInParallel()
    {
        return ProcessInParallel(null, false);
    }
    
    /// <summary>
    /// Process items in parallel without concurrency limits and return results.
    /// </summary>
    /// <param name="scheduleOnThreadPool">If true, schedules tasks on thread pool to prevent blocking. Default is false for maximum performance.</param>
    /// <returns>An async processor configured for parallel execution.</returns>
    public IAsyncEnumerableProcessor<TOutput> ProcessInParallel(bool scheduleOnThreadPool)
    {
        return ProcessInParallel(null, scheduleOnThreadPool);
    }
    
    /// <summary>
    /// Process items in parallel with specified concurrency limit and return results.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations.</param>
    /// <returns>An async processor configured for parallel execution.</returns>
    public IAsyncEnumerableProcessor<TOutput> ProcessInParallel(int maxConcurrency)
    {
        return ProcessInParallel((int?)maxConcurrency, false);
    }
    
    /// <summary>
    /// Process items in parallel with specified concurrency limit and return results.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations.</param>
    /// <returns>An async processor configured for parallel execution.</returns>
    public IAsyncEnumerableProcessor<TOutput> ProcessInParallel(int? maxConcurrency)
    {
        return ProcessInParallel(maxConcurrency, false);
    }
    
    /// <summary>
    /// Process items in parallel with specified concurrency limit and return results.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations.</param>
    /// <param name="scheduleOnThreadPool">If true, schedules tasks on thread pool to prevent blocking.</param>
    /// <returns>An async processor configured for parallel execution.</returns>
    public IAsyncEnumerableProcessor<TOutput> ProcessInParallel(int? maxConcurrency, bool scheduleOnThreadPool)
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