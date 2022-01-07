using TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class RateLimitedParallelAsyncProcessor : AbstractAsyncProcessor
{
    private readonly int _levelsOfParallelism;

    internal RateLimitedParallelAsyncProcessor(int count, Func<Task> taskSelector, int levelsOfParallelism, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
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