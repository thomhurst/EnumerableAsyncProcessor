using TomLonghurst.EnumerableAsyncProcessor.Helpers;
using TomLonghurst.EnumerableAsyncProcessor.Interfaces;
using TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

namespace TomLonghurst.EnumerableAsyncProcessor.Builders;

public class AsyncProcessorBuilderWithAction<TSource, TResult>
{
    private readonly List<Task<Task<TResult>>> _unStartedTasks;
    private readonly CancellationTokenSource _cancellationTokenSource;

    internal AsyncProcessorBuilderWithAction(IEnumerable<TSource> items, Func<TSource,Task<TResult>> taskSelector, CancellationToken cancellationToken = default)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _unStartedTasks = TaskHelper.CreateTasksWithoutStarting(items, taskSelector, _cancellationTokenSource.Token);
    }

    public IAsyncProcessor<TResult> ProcessInBatches(int batchSize)
    {
        var batchAsyncProcessor = new BatchAsyncProcessor<TResult>(_unStartedTasks, batchSize, _cancellationTokenSource);
        batchAsyncProcessor.Process();
        return batchAsyncProcessor;
    }
    
    public IAsyncProcessor<TResult> ProcessInParallel(int levelOfParallelism)
    {
        var rateLimitedParallelAsyncProcessor = new RateLimitedParallelAsyncProcessor<TResult>(_unStartedTasks, levelOfParallelism, _cancellationTokenSource);
        rateLimitedParallelAsyncProcessor.Process();
        return rateLimitedParallelAsyncProcessor;
    }
    
    public IAsyncProcessor<TResult> ProcessInParallel()
    {
        var parallelAsyncProcessor = new ParallelAsyncProcessor<TResult>(_unStartedTasks, _cancellationTokenSource);
        parallelAsyncProcessor.Process();
        return parallelAsyncProcessor;
    }
    
    public IAsyncProcessor<TResult> ProcessOneAtATime()
    {
        var oneAtATimeAsyncProcessor = new OneAtATimeAsyncProcessor<TResult>(_unStartedTasks, _cancellationTokenSource);
        oneAtATimeAsyncProcessor.Process();
        return oneAtATimeAsyncProcessor;
    }
}

public class AsyncProcessorBuilderWithAction<TSource>
{
    private readonly List<Task<Task>> _unStartedTasks;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public AsyncProcessorBuilderWithAction(IEnumerable<TSource> items, Func<TSource,Task> taskSelector, CancellationToken cancellationToken = default)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _unStartedTasks = TaskHelper.CreateTasksWithoutStarting(items, taskSelector, _cancellationTokenSource.Token);
    }

    public IAsyncProcessor ProcessInBatches(int batchSize)
    {
        var batchAsyncProcessor = new BatchAsyncProcessor(_unStartedTasks, batchSize, _cancellationTokenSource);
        batchAsyncProcessor.Process();
        return batchAsyncProcessor;
    }
    
    public IAsyncProcessor ProcessInParallel(int levelOfParallelism)
    {
        var rateLimitedParallelAsyncProcessor = new RateLimitedParallelAsyncProcessor(_unStartedTasks, levelOfParallelism, _cancellationTokenSource);
        rateLimitedParallelAsyncProcessor.Process();
        return rateLimitedParallelAsyncProcessor;
    }
    
    public IAsyncProcessor ProcessInParallel()
    {
        var parallelAsyncProcessor = new ParallelAsyncProcessor(_unStartedTasks, _cancellationTokenSource);
        parallelAsyncProcessor.Process();
        return parallelAsyncProcessor;
    }
    
    public IAsyncProcessor ProcessOneAtATime()
    {
        var oneAtATimeAsyncProcessor = new OneAtATimeAsyncProcessor(_unStartedTasks, _cancellationTokenSource);
        oneAtATimeAsyncProcessor.Process();
        return oneAtATimeAsyncProcessor;
    }
}