using EnumerableAsyncProcessor.Interfaces;
using EnumerableAsyncProcessor.RunnableProcessors;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.Builders;

public class ActionAsyncProcessorBuilder
{
    private readonly int _count;
    private readonly Func<Task> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public ActionAsyncProcessorBuilder(int count, Func<Task> taskSelector, CancellationToken cancellationToken)
    {
        _count = count;
        _taskSelector = taskSelector;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    public IAsyncProcessor ProcessInBatches(int batchSize)
    {
        return new BatchAsyncProcessor(batchSize, _count, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor ProcessInParallel(int levelOfParallelism)
    {
        return new RateLimitedParallelAsyncProcessor(_count, _taskSelector, levelOfParallelism, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor ProcessInParallel(int levelOfParallelism, TimeSpan timeSpan)
    {
        return new TimedRateLimitedParallelAsyncProcessor(_count, _taskSelector, levelOfParallelism, timeSpan, _cancellationTokenSource).StartProcessing();
    }
    
    /// <summary>
    /// Process tasks in parallel without concurrency limits.
    /// </summary>
    /// <returns>An async processor configured for parallel execution.</returns>
    public IAsyncProcessor ProcessInParallel()
    {
        return ProcessInParallel(null);
    }
    
    /// <summary>
    /// Process tasks in parallel with specified concurrency limit.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations.</param>
    /// <returns>An async processor configured for parallel execution.</returns>
    public IAsyncProcessor ProcessInParallel(int? maxConcurrency)
    {
        return new ParallelAsyncProcessor(_count, _taskSelector, _cancellationTokenSource, maxConcurrency).StartProcessing();
    }
    
    public IAsyncProcessor ProcessOneAtATime()
    {
        return new OneAtATimeAsyncProcessor(_count, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
    
    /// <summary>
    /// Process ALL tasks in parallel without any concurrency limits.
    /// WARNING: Use with caution - can overwhelm system resources with large task counts.
    /// Ideal for scenarios requiring maximum parallelism like running thousands of unit tests.
    /// </summary>
    /// <returns>An async processor with unbounded parallelism.</returns>
    public IAsyncProcessor ProcessInParallelUnbounded()
    {
        return new UnboundedParallelAsyncProcessor(_count, _taskSelector, _cancellationTokenSource).StartProcessing();
    }

}