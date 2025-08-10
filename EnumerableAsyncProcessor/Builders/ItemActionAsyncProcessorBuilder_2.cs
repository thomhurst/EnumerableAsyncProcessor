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
        return ProcessInParallel(null);
    }
    
    /// <summary>
    /// Process items in parallel with specified concurrency limit and return results.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations.</param>
    /// <returns>An async processor configured for parallel execution that returns results.</returns>
    public IAsyncProcessor<TOutput> ProcessInParallel(int? maxConcurrency)
    {
        return new ResultParallelAsyncProcessor<TInput, TOutput>(_items, _taskSelector, _cancellationTokenSource, maxConcurrency).StartProcessing();
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

}