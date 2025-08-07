using EnumerableAsyncProcessor.Interfaces;
using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.Builders;

public class ActionAsyncProcessorBuilder<TOutput>
{
    private readonly int _count;
    private readonly Func<Task<TOutput>> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    internal ActionAsyncProcessorBuilder(int count, Func<Task<TOutput>> taskSelector, CancellationToken cancellationToken)
    {
        _count = count;
        _taskSelector = taskSelector;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    public IAsyncProcessor<TOutput> ProcessInBatches(int batchSize)
    {
        return new ResultBatchAsyncProcessor<TOutput>(batchSize, _count, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor<TOutput> ProcessInParallel(int levelOfParallelism)
    {
        return new ResultRateLimitedParallelAsyncProcessor<TOutput>(_count, _taskSelector, levelOfParallelism, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor<TOutput> ProcessInParallel(int levelOfParallelism, TimeSpan timeSpan)
    {
        return new ResultTimedRateLimitedParallelAsyncProcessor<TOutput>(_count, _taskSelector, levelOfParallelism, timeSpan, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor<TOutput> ProcessInParallel()
    {
        return new ResultParallelAsyncProcessor<TOutput>(_count, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
    
    /// <summary>
    /// Process tasks in parallel with optimizations for I/O-bound operations and return results.
    /// Removes Task.Run overhead and allows higher concurrency levels.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations. If null, defaults to 10x processor count or minimum 100 for I/O-bound tasks.</param>
    /// <returns>An async processor optimized for I/O operations that returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessInParallelForIO(int? maxConcurrency = null)
    {
        return new ResultIOBoundParallelAsyncProcessor<TOutput>(_count, _taskSelector, _cancellationTokenSource, maxConcurrency).StartProcessing();
    }
    
    /// <summary>
    /// Process tasks in parallel with explicit I/O vs CPU-bound configuration and return results.
    /// </summary>
    /// <param name="isIOBound">True for I/O-bound tasks (removes Task.Run overhead), false for CPU-bound tasks.</param>
    /// <returns>An async processor configured for the specified workload type that returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessInParallel(bool isIOBound)
    {
        return new ResultParallelAsyncProcessor<TOutput>(_count, _taskSelector, _cancellationTokenSource, isIOBound).StartProcessing();
    }
    
    public IAsyncProcessor<TOutput> ProcessOneAtATime()
    {
        return new ResultOneAtATimeAsyncProcessor<TOutput>(_count, _taskSelector, _cancellationTokenSource).StartProcessing();
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Process tasks using a channel-based approach with producer-consumer pattern and return results.
    /// </summary>
    /// <param name="options">Channel configuration options. If null, uses unbounded channel with single consumer.</param>
    /// <returns>An async processor that processes tasks through a channel and returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessWithChannel(ChannelProcessorOptions? options = null)
    {
        return new ResultChannelBasedBatchAsyncProcessor<TOutput>(_count, _taskSelector, _cancellationTokenSource, options).StartProcessing();
    }
#endif
}