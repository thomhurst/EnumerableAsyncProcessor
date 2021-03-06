using TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class TimedRateLimitedParallelAsyncProcessor : AbstractAsyncProcessor
{
    private readonly int _levelsOfParallelism;
    private readonly TimeSpan _timeSpan;

    internal TimedRateLimitedParallelAsyncProcessor(int count, Func<Task> taskSelector, int levelsOfParallelism, TimeSpan timeSpan, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
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
                    Task.Delay(_timeSpan, CancellationToken)
                );
            });
    }
}