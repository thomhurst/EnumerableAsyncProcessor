using System.Collections.Immutable;
using TomLonghurst.EnumerableAsyncProcessor.Extensions;
using TomLonghurst.EnumerableAsyncProcessor.Interfaces;
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
        return new ResultBatchAsyncProcessor<TSource, TResult>(batchSize, _items, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor<TResult> ProcessInParallel(int levelOfParallelism)
    {
        return new ResultRateLimitedParallelAsyncProcessor<TSource, TResult>(_items, _taskSelector, levelOfParallelism, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor<TResult> ProcessInParallel()
    {
        return new ResultParallelAsyncProcessor<TSource, TResult>(_items, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor<TResult> ProcessOneAtATime()
    {
        return new ResultOneAtATimeAsyncProcessor<TSource, TResult>(_items, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
}