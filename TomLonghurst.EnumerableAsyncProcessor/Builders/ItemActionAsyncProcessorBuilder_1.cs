using System.Collections.Immutable;
using TomLonghurst.EnumerableAsyncProcessor.Extensions;
using TomLonghurst.EnumerableAsyncProcessor.Interfaces;
using TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

namespace TomLonghurst.EnumerableAsyncProcessor.Builders;

public class ItemActionAsyncProcessorBuilder<TInput>
{
    private readonly ImmutableList<TInput> _items;
    private readonly Func<TInput, Task> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public ItemActionAsyncProcessorBuilder(IEnumerable<TInput> items, Func<TInput,Task> taskSelector, CancellationToken cancellationToken)
    {
        _items = items.ToImmutableList();
        _taskSelector = taskSelector;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    public IAsyncProcessor ProcessInBatches(int batchSize)
    {
        return new BatchAsyncProcessor<TInput>(batchSize, _items, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor ProcessInParallel(int levelOfParallelism)
    {
        return new RateLimitedParallelAsyncProcessor<TInput>(_items, _taskSelector, levelOfParallelism, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor ProcessInParallel(int levelOfParallelism, TimeSpan timeSpan)
    {
        return new TimedRateLimitedParallelAsyncProcessor<TInput>(_items, _taskSelector, levelOfParallelism, timeSpan, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor ProcessInParallel()
    {
        return new ParallelAsyncProcessor<TInput>(_items, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor ProcessOneAtATime()
    {
        return new OneAtATimeAsyncProcessor<TInput>(_items, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
}