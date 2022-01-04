using System.Collections.Immutable;
using TomLonghurst.EnumerableAsyncProcessor.Interfaces;
using TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;
using TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

namespace TomLonghurst.EnumerableAsyncProcessor.Builders;

public class ItemActionAsyncProcessorBuilder<TSource, TResult>
{
    private readonly ImmutableList<TSource> _items;
    private readonly Func<TSource, Task<TResult>> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    internal ItemActionAsyncProcessorBuilder(IEnumerable<TSource> items, Func<TSource,Task<TResult>> taskSelector, CancellationToken cancellationToken)
    {
        _items = items.ToImmutableList();
        _taskSelector = taskSelector;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    public IAsyncProcessor<TResult> ProcessInBatches(int batchSize)
    {
        var batchAsyncProcessor = new ResultBatchAsyncProcessor<TSource, TResult>(batchSize, _items, _taskSelector, _cancellationTokenSource);
        _ = batchAsyncProcessor.Process();
        return batchAsyncProcessor;
    }
    
    public IAsyncProcessor<TResult> ProcessInParallel(int levelOfParallelism)
    {
        var rateLimitedParallelAsyncProcessor = new ResultRateLimitedParallelAsyncProcessor<TSource, TResult>(_items, _taskSelector, levelOfParallelism, _cancellationTokenSource);
        _ = rateLimitedParallelAsyncProcessor.Process();
        return rateLimitedParallelAsyncProcessor;
    }
    
    public IAsyncProcessor<TResult> ProcessInParallel()
    {
        var parallelAsyncProcessor = new ResultParallelAsyncProcessor<TSource, TResult>(_items, _taskSelector, _cancellationTokenSource);
        _ = parallelAsyncProcessor.Process();
        return parallelAsyncProcessor;
    }
    
    public IAsyncProcessor<TResult> ProcessOneAtATime()
    {
        var oneAtATimeAsyncProcessor = new ResultOneAtATimeAsyncProcessor<TSource, TResult>(_items, _taskSelector, _cancellationTokenSource);
        _ = oneAtATimeAsyncProcessor.Process();
        return oneAtATimeAsyncProcessor;
    }
}

public class ItemActionAsyncProcessorBuilder<TSource>
{
    private readonly ImmutableList<TSource> _items;
    private readonly Func<TSource, Task> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public ItemActionAsyncProcessorBuilder(IEnumerable<TSource> items, Func<TSource,Task> taskSelector, CancellationToken cancellationToken)
    {
        _items = items.ToImmutableList();
        _taskSelector = taskSelector;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    public IAsyncProcessor ProcessInBatches(int batchSize)
    {
        var batchAsyncProcessor = new BatchAsyncProcessor<TSource>(batchSize, _items, _taskSelector, _cancellationTokenSource);
        _ = batchAsyncProcessor.Process();
        return batchAsyncProcessor;
    }
    
    public IAsyncProcessor ProcessInParallel(int levelOfParallelism)
    {
        var rateLimitedParallelAsyncProcessor = new RateLimitedParallelAsyncProcessor<TSource>(_items, _taskSelector, levelOfParallelism, _cancellationTokenSource);
        _ = rateLimitedParallelAsyncProcessor.Process();
        return rateLimitedParallelAsyncProcessor;
    }
    
    public IAsyncProcessor ProcessInParallel()
    {
        var parallelAsyncProcessor = new ParallelAsyncProcessor<TSource>(_items, _taskSelector, _cancellationTokenSource);
        _ = parallelAsyncProcessor.Process();
        return parallelAsyncProcessor;
    }
    
    public IAsyncProcessor ProcessOneAtATime()
    {
        var oneAtATimeAsyncProcessor = new OneAtATimeAsyncProcessor<TSource>(_items, _taskSelector, _cancellationTokenSource);
        _ = oneAtATimeAsyncProcessor.Process();
        return oneAtATimeAsyncProcessor;
    }
}