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

    public IRunnableAsyncRegulator<TResult> ProcessInBatches(int batchSize, CancellationToken cancellationToken = default)
    {
        return new BatchAsyncProcessor<TResult>(_unStartedTasks, batchSize, cancellationToken);
    }
    
    public IRunnableAsyncRegulator<TResult> ProcessInParallel(int levelOfParallelism, CancellationToken cancellationToken = default)
    {
        return new RateLimitedParallelAsyncProcessor<TResult>(_unStartedTasks, levelOfParallelism, cancellationToken);
    }
    
    public IRunnableAsyncRegulator<TResult> ProcessInParallel(CancellationToken cancellationToken = default)
    {
        return new ParallelAsyncProcessor<TResult>(_unStartedTasks, cancellationToken);
    }
    
    public IRunnableAsyncRegulator<TResult> ProcessOneAtATime(CancellationToken cancellationToken = default)
    {
        return new OneAtATimeAsyncProcessor<TResult>(_unStartedTasks, cancellationToken);
    }
}