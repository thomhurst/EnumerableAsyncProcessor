using EnumerableAsyncProcessor.Interfaces;
using EnumerableAsyncProcessor.RunnableProcessors;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.Builders;

public sealed class ItemActionAsyncProcessorBuilder<TInput>
{
    private readonly IEnumerable<TInput> _items;
    private readonly Func<TInput, Task> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public ItemActionAsyncProcessorBuilder(IEnumerable<TInput> items, Func<TInput,Task> taskSelector, CancellationToken cancellationToken)
    {
        _items = items;
        _taskSelector = taskSelector;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    public ItemActionAsyncProcessorBuilder(IEnumerable<TInput> items, Func<TInput, CancellationToken, Task> taskSelector, CancellationToken cancellationToken)
    {
        _items = items;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _taskSelector = item => taskSelector(item, _cancellationTokenSource.Token);
    }

    public IAsyncProcessor ProcessInBatches(int batchSize)
    {
        return new BatchAsyncProcessor<TInput>(batchSize, _items, _taskSelector, _cancellationTokenSource)
            .StartProcessing();
    }
    
    public IAsyncProcessor ProcessInParallel(int maxConcurrency, TimeSpan timeSpan)
    {
        return ProcessInParallel(maxConcurrency, timeSpan, maxConcurrency);
    }

    /// <summary>Processes items with independent start-rate and concurrency limits.</summary>
    /// <param name="permitsPerWindow">Maximum operations that may start in each window.</param>
    /// <param name="window">Rate-limit replenishment window.</param>
    /// <param name="maxConcurrency">Maximum operations that may remain in flight.</param>
    public IAsyncProcessor ProcessInParallel(int permitsPerWindow, TimeSpan window, int maxConcurrency)
    {
        return new TimedRateLimitedParallelAsyncProcessor<TInput>(_items, _taskSelector, permitsPerWindow, window, maxConcurrency, _cancellationTokenSource)
            .StartProcessing();
    }
    
    /// <summary>
    /// Process items in parallel, optionally limiting concurrency.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations, or null for unbounded concurrency.</param>
    /// <param name="scheduleOnThreadPool">For unbounded processing, schedules tasks on the thread pool when true.</param>
    /// <returns>An async processor configured for parallel execution.</returns>
    public IAsyncProcessor ProcessInParallel(int? maxConcurrency = null, bool scheduleOnThreadPool = false)
    {
        return new ParallelAsyncProcessor<TInput>(_items, _taskSelector, _cancellationTokenSource, maxConcurrency, scheduleOnThreadPool)
            .StartProcessing();
    }

    public IAsyncProcessor ProcessOneAtATime()
    {
        return new OneAtATimeAsyncProcessor<TInput>(_items, _taskSelector, _cancellationTokenSource)
            .StartProcessing();
    }

}
