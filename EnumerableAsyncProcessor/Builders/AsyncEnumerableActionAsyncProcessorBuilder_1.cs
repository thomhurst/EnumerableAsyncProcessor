#if NET6_0_OR_GREATER
using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable;

namespace EnumerableAsyncProcessor.Builders;

public class AsyncEnumerableActionAsyncProcessorBuilder<TInput>
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public AsyncEnumerableActionAsyncProcessorBuilder(
        IAsyncEnumerable<TInput> items, 
        Func<TInput, Task> taskSelector, 
        CancellationToken cancellationToken)
    {
        _items = items;
        _taskSelector = taskSelector;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    /// <summary>
    /// Process items in parallel with a specified level of parallelism.
    /// </summary>
    public IAsyncEnumerableProcessor ProcessInParallel(int maxConcurrency)
    {
        return new AsyncEnumerableParallelProcessor<TInput>(
            _items, _taskSelector, maxConcurrency, _cancellationTokenSource);
    }
    
    /// <summary>
    /// Process items in parallel with default concurrency (processor count).
    /// </summary>
    public IAsyncEnumerableProcessor ProcessInParallel()
    {
        return ProcessInParallel(Environment.ProcessorCount);
    }
    
    
    /// <summary>
    /// Process items one at a time (sequential processing).
    /// </summary>
    public IAsyncEnumerableProcessor ProcessOneAtATime()
    {
        return new AsyncEnumerableOneAtATimeProcessor<TInput>(
            _items, _taskSelector, _cancellationTokenSource);
    }

}
#endif