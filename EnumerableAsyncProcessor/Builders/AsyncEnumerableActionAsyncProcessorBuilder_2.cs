#if NET6_0_OR_GREATER
using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable.ResultProcessors;

namespace EnumerableAsyncProcessor.Builders;

public class AsyncEnumerableActionAsyncProcessorBuilder<TInput, TOutput>
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task<TOutput>> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public AsyncEnumerableActionAsyncProcessorBuilder(
        IAsyncEnumerable<TInput> items, 
        Func<TInput, Task<TOutput>> taskSelector, 
        CancellationToken cancellationToken)
    {
        _items = items;
        _taskSelector = taskSelector;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    /// <summary>
    /// Process items in parallel with a specified level of parallelism and return results.
    /// </summary>
    public IAsyncEnumerableProcessor<TOutput> ProcessInParallel(int maxConcurrency)
    {
        return new ResultAsyncEnumerableParallelProcessor<TInput, TOutput>(
            _items, _taskSelector, maxConcurrency, _cancellationTokenSource);
    }
    
    /// <summary>
    /// Process items in parallel with default concurrency and return results.
    /// </summary>
    public IAsyncEnumerableProcessor<TOutput> ProcessInParallel()
    {
        return ProcessInParallel(Environment.ProcessorCount);
    }
    
    
    /// <summary>
    /// Process items one at a time and return results in order.
    /// </summary>
    public IAsyncEnumerableProcessor<TOutput> ProcessOneAtATime()
    {
        return new ResultAsyncEnumerableOneAtATimeProcessor<TInput, TOutput>(
            _items, _taskSelector, _cancellationTokenSource);
    }

    /// <summary>
    /// Process ALL items in parallel without any concurrency limits and return results.
    /// WARNING: Use with caution - can overwhelm system resources with large async enumerables.
    /// </summary>
    public IAsyncEnumerableProcessor<TOutput> ProcessInParallelUnbounded()
    {
        return new ResultAsyncEnumerableUnboundedParallelProcessor<TInput, TOutput>(
            _items, _taskSelector, _cancellationTokenSource);
    }

}
#endif