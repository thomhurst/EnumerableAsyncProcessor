using EnumerableAsyncProcessor.Interfaces;
using EnumerableAsyncProcessor.RunnableProcessors;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.Builders;

public class ItemActionAsyncProcessorBuilder<TInput>
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

    public IAsyncProcessor ProcessInBatches(int batchSize)
    {
        return new BatchAsyncProcessor<TInput>(batchSize, _items, _taskSelector, _cancellationTokenSource)
            .StartProcessing();
    }
    
    public IAsyncProcessor ProcessInParallel(int levelOfParallelism)
    {
        return new RateLimitedParallelAsyncProcessor<TInput>(_items, _taskSelector, levelOfParallelism, _cancellationTokenSource)
            .StartProcessing();
    }
    
    public IAsyncProcessor ProcessInParallel(int levelOfParallelism, TimeSpan timeSpan)
    {
        return new TimedRateLimitedParallelAsyncProcessor<TInput>(_items, _taskSelector, levelOfParallelism, timeSpan, _cancellationTokenSource)
            .StartProcessing();
    }
    
    /// <summary>
    /// Process items in parallel without concurrency limits.
    /// </summary>
    /// <returns>An async processor configured for parallel execution.</returns>
    public IAsyncProcessor ProcessInParallel()
    {
        return ProcessInParallel(null);
    }
    
    /// <summary>
    /// Process items in parallel with specified concurrency limit.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations.</param>
    /// <returns>An async processor configured for parallel execution.</returns>
    public IAsyncProcessor ProcessInParallel(int? maxConcurrency)
    {
        return new ParallelAsyncProcessor<TInput>(_items, _taskSelector, _cancellationTokenSource, maxConcurrency)
            .StartProcessing();
    }
    
    public IAsyncProcessor ProcessOneAtATime()
    {
        return new OneAtATimeAsyncProcessor<TInput>(_items, _taskSelector, _cancellationTokenSource)
            .StartProcessing();
    }
    
    /// <summary>
    /// Process ALL items in parallel without any concurrency limits.
    /// WARNING: Use with caution - can overwhelm system resources with large item counts.
    /// Ideal for scenarios requiring maximum parallelism like running thousands of unit tests.
    /// </summary>
    /// <returns>An async processor with unbounded parallelism.</returns>
    public IAsyncProcessor ProcessInParallelUnbounded()
    {
        return new UnboundedParallelAsyncProcessor<TInput>(_items, _taskSelector, _cancellationTokenSource)
            .StartProcessing();
    }

}