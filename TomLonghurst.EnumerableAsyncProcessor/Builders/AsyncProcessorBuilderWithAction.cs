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

    public IRunnableAsyncRegulator<TResult> ProcessInBatches(int batchSize)
    {
        return new BatchAsyncProcessor<TResult>(_unStartedTasks, batchSize);
    }
    
    public IRunnableAsyncRegulator<TResult> ProcessInParallel(int levelOfParallelism)
    {
        return new RateLimitedParallelAsyncProcessor<TResult>(_unStartedTasks, levelOfParallelism);
    }
    
    public IRunnableAsyncRegulator<TResult> ProcessInParallel()
    {
        return new ParallelAsyncProcessor<TResult>(_unStartedTasks);
    }
    
    public IRunnableAsyncRegulator<TResult> ProcessOneAtATime()
    {
        return new OneAtATimeAsyncProcessor<TResult>(_unStartedTasks);
    }
}