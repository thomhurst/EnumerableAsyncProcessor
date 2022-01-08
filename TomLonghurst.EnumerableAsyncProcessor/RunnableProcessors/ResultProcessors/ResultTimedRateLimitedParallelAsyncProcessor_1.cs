using TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultTimedRateLimitedParallelAsyncProcessor<TOutput> : ResultAbstractAsyncProcessor<TOutput>
{
    private readonly int _levelsOfParallelism;
    private readonly TimeSpan _timeSpan;

    internal ResultTimedRateLimitedParallelAsyncProcessor(int count, Func<Task<TOutput>> taskSelector, int levelsOfParallelism, TimeSpan timeSpan, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
        _levelsOfParallelism = levelsOfParallelism;
        _timeSpan = timeSpan;
    }

    internal override Task Process()
    {
        return Parallel.ForEachAsync(EnumerableTaskCompletionSources,
            new ParallelOptions
                { MaxDegreeOfParallelism = _levelsOfParallelism, CancellationToken = CancellationToken },
            async (taskCompletionSource, _) =>
            {
                await Task.WhenAll(
                    ProcessItem(taskCompletionSource),
                    Task.Delay(_timeSpan, CancellationToken));
            });
    }
}