using TomLonghurst.EnumerableAsyncProcessor.Helpers;
using TomLonghurst.EnumerableAsyncProcessor.Interfaces;
using TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;
using TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

namespace TomLonghurst.EnumerableAsyncProcessor.Builders;

public class ActionAsyncProcessorBuilder<TResult>
{
    private readonly int _count;
    private readonly Func<Task<TResult>> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    internal ActionAsyncProcessorBuilder(int count, Func<Task<TResult>> taskSelector, CancellationToken cancellationToken = default)
    {
        _count = count;
        _taskSelector = taskSelector;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    public IAsyncProcessor<TResult> ProcessInBatches(int batchSize)
    {
        var batchAsyncProcessor = new ResultBatchAsyncProcessor<TResult>(batchSize, _count, _taskSelector, _cancellationTokenSource);
        batchAsyncProcessor.Process();
        return batchAsyncProcessor;
    }
    
    public IAsyncProcessor<TResult> ProcessInParallel(int levelOfParallelism)
    {
        var rateLimitedParallelAsyncProcessor = new ResultRateLimitedParallelAsyncProcessor<TResult>(_count, _taskSelector, levelOfParallelism, _cancellationTokenSource);
        rateLimitedParallelAsyncProcessor.Process();
        return rateLimitedParallelAsyncProcessor;
    }
    
    public IAsyncProcessor<TResult> ProcessInParallel()
    {
        var parallelAsyncProcessor = new ResultParallelAsyncProcessor<TResult>(_count, _taskSelector, _cancellationTokenSource);
        parallelAsyncProcessor.Process();
        return parallelAsyncProcessor;
    }
    
    public IAsyncProcessor<TResult> ProcessOneAtATime()
    {
        var oneAtATimeAsyncProcessor = new ResultOneAtATimeAsyncProcessor<TResult>(_count, _taskSelector, _cancellationTokenSource);
        oneAtATimeAsyncProcessor.Process();
        return oneAtATimeAsyncProcessor;
    }
}

public class ActionAsyncProcessorBuilder
{
    private readonly int _count;
    private readonly Func<Task> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public ActionAsyncProcessorBuilder(int count, Func<Task> taskSelector, CancellationToken cancellationToken = default)
    {
        _count = count;
        _taskSelector = taskSelector;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    public IAsyncProcessor ProcessInBatches(int batchSize)
    {
        var batchAsyncProcessor = new BatchAsyncProcessor(batchSize, _count, _taskSelector, _cancellationTokenSource);
        batchAsyncProcessor.Process();
        return batchAsyncProcessor;
    }
    
    public IAsyncProcessor ProcessInParallel(int levelOfParallelism)
    {
        var rateLimitedParallelAsyncProcessor = new RateLimitedParallelAsyncProcessor(_count, _taskSelector, levelOfParallelism, _cancellationTokenSource);
        rateLimitedParallelAsyncProcessor.Process();
        return rateLimitedParallelAsyncProcessor;
    }
    
    public IAsyncProcessor ProcessInParallel()
    {
        var parallelAsyncProcessor = new ParallelAsyncProcessor(_count, _taskSelector, _cancellationTokenSource);
        parallelAsyncProcessor.Process();
        return parallelAsyncProcessor;
    }
    
    public IAsyncProcessor ProcessOneAtATime()
    {
        var oneAtATimeAsyncProcessor = new OneAtATimeAsyncProcessor(_count, _taskSelector, _cancellationTokenSource);
        oneAtATimeAsyncProcessor.Process();
        return oneAtATimeAsyncProcessor;
    }
}