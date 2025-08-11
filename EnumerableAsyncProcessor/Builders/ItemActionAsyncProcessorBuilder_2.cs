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
    
    /// <summary>
    /// Process items in parallel without concurrency limits and return results.
    /// </summary>
    /// <returns>An async processor configured for parallel execution that returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessInParallel()
    {
        return ProcessInParallel(null, false);
    }
    
    /// <summary>
    /// Process items in parallel without concurrency limits and return results.
    /// </summary>
    /// <param name="scheduleOnThreadPool">If true, schedules tasks on thread pool to prevent blocking. Default is false for maximum performance.</param>
    /// <returns>An async processor configured for parallel execution that returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessInParallel(bool scheduleOnThreadPool)
    {
        return ProcessInParallel(null, scheduleOnThreadPool);
    }
    
    /// <summary>
    /// Process items in parallel with specified concurrency limit and return results.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations.</param>
    /// <returns>An async processor configured for parallel execution that returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessInParallel(int? maxConcurrency)
    {
        return ProcessInParallel(maxConcurrency, false);
    }
    
    /// <summary>
    /// Process items in parallel with specified concurrency limit and return results.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations.</param>
    /// <param name="scheduleOnThreadPool">If true, schedules tasks on thread pool to prevent blocking.</param>
    /// <returns>An async processor configured for parallel execution that returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessInParallel(int? maxConcurrency, bool scheduleOnThreadPool)
    {
        return new ResultParallelAsyncProcessor<TInput, TOutput>(_items, _taskSelector, _cancellationTokenSource, maxConcurrency, scheduleOnThreadPool).StartProcessing();
    }
    
    public IAsyncProcessor<TOutput> ProcessOneAtATime()
    {
        return new ResultOneAtATimeAsyncProcessor<TInput, TOutput>(_items, _taskSelector, _cancellationTokenSource).StartProcessing();
    }

}