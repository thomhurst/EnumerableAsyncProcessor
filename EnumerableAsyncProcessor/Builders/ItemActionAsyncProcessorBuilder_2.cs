using EnumerableAsyncProcessor.Interfaces;
using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.Builders;

public sealed class ItemActionAsyncProcessorBuilder<TInput, TOutput>
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

    internal ItemActionAsyncProcessorBuilder(IEnumerable<TInput> items, Func<TInput, CancellationToken, Task<TOutput>> taskSelector, CancellationToken cancellationToken)
    {
        _items = items;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _taskSelector = item => taskSelector(item, _cancellationTokenSource.Token);
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
    /// Processes items in parallel with the specified concurrency and time constraints.
    /// </summary>
    /// <param name="maxConcurrency">The maximum number of concurrent operations.</param>
    /// <param name="timeSpan">The time span constraint for rate limiting.</param>
    /// <returns>An async processor that implements IDisposable and IAsyncDisposable. 
    /// Use 'await using' or proper disposal to ensure resources are cleaned up.</returns>
    /// <remarks>
    /// The returned processor should be disposed to ensure proper cleanup of internal resources 
    /// and cancellation of running tasks. Use 'await using var processor = ...' for automatic disposal.
    /// </remarks>
    public IAsyncProcessor<TOutput> ProcessInParallel(int maxConcurrency, TimeSpan timeSpan)
    {
        return ProcessInParallel(maxConcurrency, timeSpan, maxConcurrency);
    }

    /// <summary>
    /// Processes items with independent start-rate and concurrency limits.
    /// </summary>
    /// <param name="permitsPerWindow">Maximum operations that may start in each window.</param>
    /// <param name="window">Rate-limit replenishment window.</param>
    /// <param name="maxConcurrency">Maximum operations that may remain in flight.</param>
    /// <returns>An async processor that implements IDisposable and IAsyncDisposable.</returns>
    public IAsyncProcessor<TOutput> ProcessInParallel(int permitsPerWindow, TimeSpan window, int maxConcurrency)
    {
        return new ResultTimedRateLimitedParallelAsyncProcessor<TInput, TOutput>(_items, _taskSelector, permitsPerWindow, window, maxConcurrency, _cancellationTokenSource).StartProcessing();
    }
    
    /// <summary>
    /// Process items in parallel, optionally limiting concurrency, and return results.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations, or null for unbounded concurrency.</param>
    /// <param name="scheduleOnThreadPool">For unbounded processing, schedules tasks on the thread pool when true.</param>
    /// <returns>An async processor that implements IDisposable and IAsyncDisposable. 
    /// Use 'await using' or proper disposal to ensure resources are cleaned up.</returns>
    /// <remarks>
    /// The returned processor should be disposed to ensure proper cleanup of internal resources 
    /// and cancellation of running tasks. Use 'await using var processor = ...' for automatic disposal.
    /// </remarks>
    public IAsyncProcessor<TOutput> ProcessInParallel(int? maxConcurrency = null, bool scheduleOnThreadPool = false)
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
