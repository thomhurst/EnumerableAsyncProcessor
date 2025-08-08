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

    public IAsyncProcessor<TOutput> ProcessInBatches(int batchSize)
    {
        return new ResultBatchAsyncProcessor<TInput, TOutput>(batchSize, _items, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor<TOutput> ProcessInParallel(int levelOfParallelism)
    {
        return new ResultRateLimitedParallelAsyncProcessor<TInput, TOutput>(_items, _taskSelector, levelOfParallelism, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor<TOutput> ProcessInParallel(int levelOfParallelism, TimeSpan timeSpan)
    {
        return new ResultTimedRateLimitedParallelAsyncProcessor<TInput, TOutput>(_items, _taskSelector, levelOfParallelism, timeSpan, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor<TOutput> ProcessInParallel()
    {
        return new ResultParallelAsyncProcessor<TInput, TOutput>(_items, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
    
    /// <summary>
    /// Process items in parallel with optimizations for I/O-bound tasks and return results.
    /// Removes Task.Run overhead and allows higher concurrency levels.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations. If null, defaults to 10x processor count or minimum 100 for I/O-bound tasks.</param>
    /// <returns>An async processor optimized for I/O operations that returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessInParallelForIO(int? maxConcurrency = null)
    {
        return new ResultIOBoundParallelAsyncProcessor<TInput, TOutput>(_items, _taskSelector, _cancellationTokenSource, maxConcurrency).StartProcessing();
    }
    
    /// <summary>
    /// Process items in parallel with explicit I/O vs CPU-bound configuration and return results.
    /// </summary>
    /// <param name="isIOBound">True for I/O-bound tasks (removes Task.Run overhead), false for CPU-bound tasks.</param>
    /// <returns>An async processor configured for the specified workload type that returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessInParallel(bool isIOBound)
    {
        return new ResultParallelAsyncProcessor<TInput, TOutput>(_items, _taskSelector, _cancellationTokenSource, isIOBound).StartProcessing();
    }
    
    public IAsyncProcessor<TOutput> ProcessOneAtATime()
    {
        return new ResultOneAtATimeAsyncProcessor<TInput, TOutput>(_items, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
    
    /// <summary>
    /// Process ALL items in parallel without any concurrency limits and return results.
    /// WARNING: Use with caution - can overwhelm system resources with large item counts.
    /// Ideal for scenarios requiring maximum parallelism like running thousands of unit tests.
    /// </summary>
    /// <returns>An async processor with unbounded parallelism that returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessInParallelUnbounded()
    {
        return new ResultUnboundedParallelAsyncProcessor<TInput, TOutput>(_items, _taskSelector, _cancellationTokenSource).StartProcessing();
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Process items using a channel-based approach with producer-consumer pattern and return results.
    /// </summary>
    /// <param name="options">Channel configuration options. If null, uses unbounded channel with single consumer.</param>
    /// <returns>An async processor that processes items through a channel and returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessWithChannel(ChannelProcessorOptions? options = null)
    {
        return new ResultChannelBasedBatchAsyncProcessor<TInput, TOutput>(_items, _taskSelector, _cancellationTokenSource, options).StartProcessing();
    }
#endif
}