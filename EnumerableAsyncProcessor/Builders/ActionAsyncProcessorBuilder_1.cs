using EnumerableAsyncProcessor.Interfaces;
using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.Builders;

public sealed class ActionAsyncProcessorBuilder<TOutput>
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

    internal ActionAsyncProcessorBuilder(int count, Func<CancellationToken, Task<TOutput>> taskSelector, CancellationToken cancellationToken)
    {
        _count = count;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _taskSelector = () => taskSelector(_cancellationTokenSource.Token);
    }

    public IAsyncProcessor<TOutput> ProcessInBatches(int batchSize)
    {
        return new ResultBatchAsyncProcessor<TOutput>(batchSize, _count, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor<TOutput> ProcessInParallel(int maxConcurrency, TimeSpan timeSpan)
    {
        return ProcessInParallel(maxConcurrency, timeSpan, maxConcurrency);
    }

    /// <summary>Processes tasks with independent start-rate and concurrency limits.</summary>
    /// <param name="permitsPerWindow">Maximum operations that may start in each window.</param>
    /// <param name="window">Rate-limit replenishment window.</param>
    /// <param name="maxConcurrency">Maximum operations that may remain in flight.</param>
    public IAsyncProcessor<TOutput> ProcessInParallel(int permitsPerWindow, TimeSpan window, int maxConcurrency)
    {
        return new ResultTimedRateLimitedParallelAsyncProcessor<TOutput>(_count, _taskSelector, permitsPerWindow, window, maxConcurrency, _cancellationTokenSource).StartProcessing();
    }
    
    /// <summary>
    /// Process tasks in parallel, optionally limiting concurrency, and return results.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations, or null for unbounded concurrency.</param>
    /// <param name="scheduleOnThreadPool">For unbounded processing, schedules tasks on the thread pool when true.</param>
    /// <returns>An async processor configured for parallel execution that returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessInParallel(int? maxConcurrency = null, bool scheduleOnThreadPool = false)
    {
        return new ResultParallelAsyncProcessor<TOutput>(_count, _taskSelector, _cancellationTokenSource, maxConcurrency, scheduleOnThreadPool).StartProcessing();
    }

    public IAsyncProcessor<TOutput> ProcessOneAtATime()
    {
        return new ResultOneAtATimeAsyncProcessor<TOutput>(_count, _taskSelector, _cancellationTokenSource).StartProcessing();
    }

}
