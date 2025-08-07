using EnumerableAsyncProcessor.Interfaces;
using EnumerableAsyncProcessor.RunnableProcessors;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.Builders;

public class ItemActionAsyncProcessorBuilder<TInput>
{
    private readonly IEnumerable<TInput> _items;
    private readonly Func<TInput, Task> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public ItemActionAsyncProcessorBuilder(IEnumerable<TInput> items, Func<TInput,Task> taskSelector, CancellationToken cancellationToken)
    {
        _items = items;
        _taskSelector = taskSelector;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    public IAsyncProcessor ProcessInBatches(int batchSize)
    {
        return new BatchAsyncProcessor<TInput>(batchSize, _items, _taskSelector, _cancellationTokenSource)
            .StartProcessing();
    }
    
    public IAsyncProcessor ProcessInParallel(int levelOfParallelism)
    {
        return new RateLimitedParallelAsyncProcessor<TInput>(_items, _taskSelector, levelOfParallelism, _cancellationTokenSource)
            .StartProcessing();
    }
    
    public IAsyncProcessor ProcessInParallel(int levelOfParallelism, TimeSpan timeSpan)
    {
        return new TimedRateLimitedParallelAsyncProcessor<TInput>(_items, _taskSelector, levelOfParallelism, timeSpan, _cancellationTokenSource)
            .StartProcessing();
    }
    
    public IAsyncProcessor ProcessInParallel()
    {
        return new ParallelAsyncProcessor<TInput>(_items, _taskSelector, _cancellationTokenSource)
            .StartProcessing();
    }
    
    public IAsyncProcessor ProcessOneAtATime()
    {
        return new OneAtATimeAsyncProcessor<TInput>(_items, _taskSelector, _cancellationTokenSource)
            .StartProcessing();
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Process items using a channel-based approach with producer-consumer pattern.
    /// </summary>
    /// <param name="options">Channel configuration options. If null, uses unbounded channel with single consumer.</param>
    /// <returns>An async processor that processes items through a channel.</returns>
    public IAsyncProcessor ProcessWithChannel(ChannelProcessorOptions? options = null)
    {
        return new ChannelBasedBatchAsyncProcessor<TInput>(_items, _taskSelector, _cancellationTokenSource, options)
            .StartProcessing();
    }
#endif
}