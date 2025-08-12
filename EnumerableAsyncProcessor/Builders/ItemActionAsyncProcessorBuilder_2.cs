using EnumerableAsyncProcessor.Interfaces;
using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.Builders;

public class ItemActionAsyncProcessorBuilder<TInput, TOutput>
{
    private readonly IEnumerable<TInput> _items;
    private readonly Func<TInput, Task<TOutput>> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    internal ItemActionAsyncProcessorBuilder(IEnumerable<TInput> items, Func<TInput,Task<TOutput>> taskSelector, CancellationToken cancellationToken)
    {
        _items = items;
        _taskSelector = taskSelector;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    /// <summary>
    /// Processes items in batches of the specified size.
    /// </summary>
    /// <param name="batchSize">The number of items to process in each batch.</param>
    /// <returns>An async processor that implements IDisposable and IAsyncDisposable. 
    /// Use 'await using' or proper disposal to ensure resources are cleaned up.</returns>
    /// <remarks>
    /// The returned processor should be disposed to ensure proper cleanup of internal resources 
    /// and cancellation of running tasks. Use 'await using var processor = ...' for automatic disposal.
    /// </remarks>
    public IAsyncProcessor<TOutput> ProcessInBatches(int batchSize)
    {
        return new ResultBatchAsyncProcessor<TInput, TOutput>(batchSize, _items, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
    
    /// <summary>
    /// Processes items in parallel with the specified level of parallelism.
    /// </summary>
    /// <param name="levelOfParallelism">The maximum number of concurrent operations.</param>
    /// <returns>An async processor that implements IDisposable and IAsyncDisposable. 
    /// Use 'await using' or proper disposal to ensure resources are cleaned up.</returns>
    /// <remarks>
    /// The returned processor should be disposed to ensure proper cleanup of internal resources 
    /// and cancellation of running tasks. Use 'await using var processor = ...' for automatic disposal.
    /// </remarks>
    public IAsyncProcessor<TOutput> ProcessInParallel(int levelOfParallelism)
    {
        return new ResultRateLimitedParallelAsyncProcessor<TInput, TOutput>(_items, _taskSelector, levelOfParallelism, _cancellationTokenSource).StartProcessing();
    }
    
    /// <summary>
    /// Processes items in parallel with the specified level of parallelism and time constraints.
    /// </summary>
    /// <param name="levelOfParallelism">The maximum number of concurrent operations.</param>
    /// <param name="timeSpan">The time span constraint for rate limiting.</param>
    /// <returns>An async processor that implements IDisposable and IAsyncDisposable. 
    /// Use 'await using' or proper disposal to ensure resources are cleaned up.</returns>
    /// <remarks>
    /// The returned processor should be disposed to ensure proper cleanup of internal resources 
    /// and cancellation of running tasks. Use 'await using var processor = ...' for automatic disposal.
    /// </remarks>
    public IAsyncProcessor<TOutput> ProcessInParallel(int levelOfParallelism, TimeSpan timeSpan)
    {
        return new ResultTimedRateLimitedParallelAsyncProcessor<TInput, TOutput>(_items, _taskSelector, levelOfParallelism, timeSpan, _cancellationTokenSource).StartProcessing();
    }
    
    /// <summary>
    /// Process items in parallel without concurrency limits and return results.
    /// </summary>
    /// <returns>An async processor that implements IDisposable and IAsyncDisposable. 
    /// Use 'await using' or proper disposal to ensure resources are cleaned up.</returns>
    /// <remarks>
    /// The returned processor should be disposed to ensure proper cleanup of internal resources 
    /// and cancellation of running tasks. Use 'await using var processor = ...' for automatic disposal.
    /// </remarks>
    public IAsyncProcessor<TOutput> ProcessInParallel()
    {
        return ProcessInParallel(null, false);
    }
    
    /// <summary>
    /// Process items in parallel without concurrency limits and return results.
    /// </summary>
    /// <param name="scheduleOnThreadPool">If true, schedules tasks on thread pool to prevent blocking. Default is false for maximum performance.</param>
    /// <returns>An async processor that implements IDisposable and IAsyncDisposable. 
    /// Use 'await using' or proper disposal to ensure resources are cleaned up.</returns>
    /// <remarks>
    /// The returned processor should be disposed to ensure proper cleanup of internal resources 
    /// and cancellation of running tasks. Use 'await using var processor = ...' for automatic disposal.
    /// </remarks>
    public IAsyncProcessor<TOutput> ProcessInParallel(bool scheduleOnThreadPool)
    {
        return ProcessInParallel(null, scheduleOnThreadPool);
    }
    
    /// <summary>
    /// Process items in parallel with specified concurrency limit and return results.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations.</param>
    /// <returns>An async processor that implements IDisposable and IAsyncDisposable. 
    /// Use 'await using' or proper disposal to ensure resources are cleaned up.</returns>
    /// <remarks>
    /// The returned processor should be disposed to ensure proper cleanup of internal resources 
    /// and cancellation of running tasks. Use 'await using var processor = ...' for automatic disposal.
    /// </remarks>
    public IAsyncProcessor<TOutput> ProcessInParallel(int? maxConcurrency)
    {
        return ProcessInParallel(maxConcurrency, false);
    }
    
    /// <summary>
    /// Process items in parallel with specified concurrency limit and return results.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations.</param>
    /// <param name="scheduleOnThreadPool">If true, schedules tasks on thread pool to prevent blocking.</param>
    /// <returns>An async processor that implements IDisposable and IAsyncDisposable. 
    /// Use 'await using' or proper disposal to ensure resources are cleaned up.</returns>
    /// <remarks>
    /// The returned processor should be disposed to ensure proper cleanup of internal resources 
    /// and cancellation of running tasks. Use 'await using var processor = ...' for automatic disposal.
    /// </remarks>
    public IAsyncProcessor<TOutput> ProcessInParallel(int? maxConcurrency, bool scheduleOnThreadPool)
    {
        return new ResultParallelAsyncProcessor<TInput, TOutput>(_items, _taskSelector, _cancellationTokenSource, maxConcurrency, scheduleOnThreadPool).StartProcessing();
    }
    
    /// <summary>
    /// Process items one at a time sequentially.
    /// </summary>
    /// <returns>An async processor that implements IDisposable and IAsyncDisposable. 
    /// Use 'await using' or proper disposal to ensure resources are cleaned up.</returns>
    /// <remarks>
    /// The returned processor should be disposed to ensure proper cleanup of internal resources 
    /// and cancellation of running tasks. Use 'await using var processor = ...' for automatic disposal.
    /// </remarks>
    public IAsyncProcessor<TOutput> ProcessOneAtATime()
    {
        return new ResultOneAtATimeAsyncProcessor<TInput, TOutput>(_items, _taskSelector, _cancellationTokenSource).StartProcessing();
    }

}