using TomLonghurst.EnumerableAsyncProcessor.Helpers;
using TomLonghurst.EnumerableAsyncProcessor.Interfaces;
using TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

namespace TomLonghurst.EnumerableAsyncProcessor.Builders;

public class AsyncProcessorBuilderWithAction<TSource, TResult>
{
    private readonly List<Task<Task<TResult>>> _unStartedTasks;

    public AsyncProcessorBuilderWithAction(IEnumerable<TSource> items, Func<TSource,Task<TResult>> taskSelector)
    {
        _unStartedTasks = TaskHelper.CreateTasksWithoutStarting(items, taskSelector);
    }

    public IAsyncProcessor<TResult> ProcessInBatches(int batchSize, CancellationToken cancellationToken = default)
    {
        var batchAsyncProcessor = new BatchAsyncProcessor<TResult>(_unStartedTasks, batchSize, cancellationToken);
        batchAsyncProcessor.Process();
        return batchAsyncProcessor;
    }
    
    public IAsyncProcessor<TResult> ProcessInParallel(int levelOfParallelism, CancellationToken cancellationToken = default)
    {
        var rateLimitedParallelAsyncProcessor = new RateLimitedParallelAsyncProcessor<TResult>(_unStartedTasks, levelOfParallelism, cancellationToken);
        rateLimitedParallelAsyncProcessor.Process();
        return rateLimitedParallelAsyncProcessor;
    }
    
    public IAsyncProcessor<TResult> ProcessInParallel(CancellationToken cancellationToken = default)
    {
        var parallelAsyncProcessor = new ParallelAsyncProcessor<TResult>(_unStartedTasks, cancellationToken);
        parallelAsyncProcessor.Process();
        return parallelAsyncProcessor;
    }
    
    public IAsyncProcessor<TResult> ProcessOneAtATime(CancellationToken cancellationToken = default)
    {
        var oneAtATimeAsyncProcessor = new OneAtATimeAsyncProcessor<TResult>(_unStartedTasks, cancellationToken);
        oneAtATimeAsyncProcessor.Process();
        return oneAtATimeAsyncProcessor;
    }
}