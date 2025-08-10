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
    
    /// <summary>
    /// Process tasks in parallel without concurrency limits and return results.
    /// </summary>
    /// <returns>An async processor configured for parallel execution that returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessInParallel()
    {
        return ProcessInParallel(null);
    }
    
    /// <summary>
    /// Process tasks in parallel with specified concurrency limit and return results.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations.</param>
    /// <returns>An async processor configured for parallel execution that returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessInParallel(int? maxConcurrency)
    {
        return new ResultParallelAsyncProcessor<TOutput>(_count, _taskSelector, _cancellationTokenSource, maxConcurrency).StartProcessing();
    }
    
    public IAsyncProcessor<TOutput> ProcessOneAtATime()
    {
        return new ResultOneAtATimeAsyncProcessor<TOutput>(_count, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
    
    /// <summary>
    /// Process ALL tasks in parallel without any concurrency limits and return results.
    /// WARNING: Use with caution - can overwhelm system resources with large task counts.
    /// Ideal for scenarios requiring maximum parallelism like running thousands of unit tests.
    /// </summary>
    /// <returns>An async processor with unbounded parallelism that returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessInParallelUnbounded()
    {
        return new ResultUnboundedParallelAsyncProcessor<TOutput>(_count, _taskSelector, _cancellationTokenSource).StartProcessing();
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