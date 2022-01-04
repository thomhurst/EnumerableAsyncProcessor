
namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultRateLimitedParallelAsyncProcessor<TSource, TResult> : ResultAbstractAsyncProcessor<TSource, TResult>
{
    private readonly int _levelsOfParallelism;
    
    public ResultRateLimitedParallelAsyncProcessor(IReadOnlyCollection<TSource> items, Func<TSource, Task<TResult>> taskSelector, int levelsOfParallelism, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
        _levelsOfParallelism = levelsOfParallelism;
    }

    internal override Task Process()
    {
        return Parallel.ForEachAsync(ItemisedTaskCompletionSourceContainers,
            new ParallelOptions { MaxDegreeOfParallelism = _levelsOfParallelism, CancellationToken = CancellationToken},
            async (itemisedTaskCompletionSourceContainer, token) =>
            {
                try
                {
                    var result = await TaskSelector(itemisedTaskCompletionSourceContainer.Item);
                    itemisedTaskCompletionSourceContainer.TaskCompletionSource.SetResult(result);
                }
                catch (Exception e)
                {
                    itemisedTaskCompletionSourceContainer.TaskCompletionSource.SetException(e);
                }
            });
    }
}

public class ResultRateLimitedParallelAsyncProcessor<TResult> : ResultAbstractAsyncProcessor<TResult>
{
    private readonly int _levelsOfParallelism;

    public ResultRateLimitedParallelAsyncProcessor(int count, Func<Task<TResult>> taskSelector, int levelsOfParallelism, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
        _levelsOfParallelism = levelsOfParallelism;
    }

    internal override Task Process()
    {
        return Parallel.ForEachAsync(EnumerableTaskCompletionSources,
            new ParallelOptions
                { MaxDegreeOfParallelism = _levelsOfParallelism, CancellationToken = CancellationToken },
            async (taskCompletionSource, token) =>
            {
                try
                {
                    var result = await TaskSelector();
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            });
    }
}