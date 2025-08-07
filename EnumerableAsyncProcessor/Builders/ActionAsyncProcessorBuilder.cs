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
    
    public IAsyncProcessor ProcessInParallel()
    {
        return new ParallelAsyncProcessor(_count, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
    
    /// <summary>
    /// Process tasks in parallel with optimizations for I/O-bound operations.
    /// Removes Task.Run overhead and allows higher concurrency levels.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations. If null, defaults to 10x processor count or minimum 100 for I/O-bound tasks.</param>
    /// <returns>An async processor optimized for I/O operations.</returns>
    public IAsyncProcessor ProcessInParallelForIO(int? maxConcurrency = null)
    {
        return new IOBoundParallelAsyncProcessor(_count, _taskSelector, _cancellationTokenSource, maxConcurrency).StartProcessing();
    }
    
    /// <summary>
    /// Process tasks in parallel with explicit I/O vs CPU-bound configuration.
    /// </summary>
    /// <param name="isIOBound">True for I/O-bound tasks (removes Task.Run overhead), false for CPU-bound tasks.</param>
    /// <returns>An async processor configured for the specified workload type.</returns>
    public IAsyncProcessor ProcessInParallel(bool isIOBound)
    {
        return new ParallelAsyncProcessor(_count, _taskSelector, _cancellationTokenSource, isIOBound).StartProcessing();
    }
    
    public IAsyncProcessor ProcessOneAtATime()
    {
        return new OneAtATimeAsyncProcessor(_count, _taskSelector, _cancellationTokenSource).StartProcessing();
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Process tasks using a channel-based approach with producer-consumer pattern.
    /// </summary>
    /// <param name="options">Channel configuration options. If null, uses unbounded channel with single consumer.</param>
    /// <returns>An async processor that processes tasks through a channel.</returns>
    public IAsyncProcessor ProcessWithChannel(ChannelProcessorOptions? options = null)
    {
        return new ChannelBasedBatchAsyncProcessor(_count, _taskSelector, _cancellationTokenSource, options).StartProcessing();
    }
#endif
}