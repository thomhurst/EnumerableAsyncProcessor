using TomLonghurst.EnumerableAsyncProcessor.Extensions;
using TomLonghurst.EnumerableAsyncProcessor.Interfaces;
using TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

namespace TomLonghurst.EnumerableAsyncProcessor.Builders;

public class ActionAsyncProcessorBuilder<TResult>
{
    private readonly int _count;
    private readonly Func<Task<TResult>> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    internal ActionAsyncProcessorBuilder(int count, Func<Task<TResult>> taskSelector, CancellationToken cancellationToken)
    {
        _count = count;
        _taskSelector = taskSelector;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    public IAsyncProcessor<TResult> ProcessInBatches(int batchSize)
    {
        return new ResultBatchAsyncProcessor<TResult>(batchSize, _count, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor<TResult> ProcessInParallel(int levelOfParallelism)
    {
        return new ResultRateLimitedParallelAsyncProcessor<TResult>(_count, _taskSelector, levelOfParallelism, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor<TResult> ProcessInParallel()
    {
        return new ResultParallelAsyncProcessor<TResult>(_count, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor<TResult> ProcessOneAtATime()
    {
        return new ResultOneAtATimeAsyncProcessor<TResult>(_count, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
}