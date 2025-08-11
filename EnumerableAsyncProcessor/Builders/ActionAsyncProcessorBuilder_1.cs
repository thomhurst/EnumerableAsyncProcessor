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
        return ProcessInParallel(null, false);
    }
    
    /// <summary>
    /// Process tasks in parallel without concurrency limits and return results.
    /// </summary>
    /// <param name="scheduleOnThreadPool">If true, schedules tasks on thread pool to prevent blocking. Default is false for maximum performance.</param>
    /// <returns>An async processor configured for parallel execution that returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessInParallel(bool scheduleOnThreadPool)
    {
        return ProcessInParallel(null, scheduleOnThreadPool);
    }
    
    /// <summary>
    /// Process tasks in parallel with specified concurrency limit and return results.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations.</param>
    /// <returns>An async processor configured for parallel execution that returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessInParallel(int? maxConcurrency)
    {
        return ProcessInParallel(maxConcurrency, false);
    }
    
    /// <summary>
    /// Process tasks in parallel with specified concurrency limit and return results.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations.</param>
    /// <param name="scheduleOnThreadPool">If true, schedules tasks on thread pool to prevent blocking.</param>
    /// <returns>An async processor configured for parallel execution that returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessInParallel(int? maxConcurrency, bool scheduleOnThreadPool)
    {
        return new ResultParallelAsyncProcessor<TOutput>(_count, _taskSelector, _cancellationTokenSource, maxConcurrency, scheduleOnThreadPool).StartProcessing();
    }
    
    public IAsyncProcessor<TOutput> ProcessOneAtATime()
    {
        return new ResultOneAtATimeAsyncProcessor<TOutput>(_count, _taskSelector, _cancellationTokenSource).StartProcessing();
    }

}