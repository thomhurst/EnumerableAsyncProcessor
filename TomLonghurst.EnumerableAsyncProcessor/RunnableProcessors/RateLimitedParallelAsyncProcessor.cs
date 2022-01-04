using System.Collections.Immutable;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class RateLimitedParallelAsyncProcessor<TSource> : AbstractAsyncProcessor<TSource>
{
    private readonly int _levelsOfParallelism;
    
    public RateLimitedParallelAsyncProcessor(ImmutableList<TSource> items, Func<TSource, Task> taskSelector, int levelsOfParallelism, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
        _levelsOfParallelism = levelsOfParallelism;
    }

    internal override Task Process()
    {
        return Parallel.ForEachAsync(ItemisedTaskCompletionSourceContainers,
            new ParallelOptions { MaxDegreeOfParallelism = _levelsOfParallelism, CancellationToken = CancellationToken},
            async (itemisedTaskCompletionSourceContainer, _) =>
            {
                await ProcessItem(itemisedTaskCompletionSourceContainer);
            });
    }
}

public class RateLimitedParallelAsyncProcessor : AbstractAsyncProcessor
{
    private readonly int _levelsOfParallelism;

    public RateLimitedParallelAsyncProcessor(int count, Func<Task> taskSelector, int levelsOfParallelism, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
        _levelsOfParallelism = levelsOfParallelism;
    }

    internal override Task Process()
    {
        return Parallel.ForEachAsync(EnumerableTaskCompletionSources,
            new ParallelOptions
                { MaxDegreeOfParallelism = _levelsOfParallelism, CancellationToken = CancellationToken },
            async (taskCompletionSource, _) =>
            {
                await ProcessItem(taskCompletionSource);
            });
    }
}