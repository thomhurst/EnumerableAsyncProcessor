using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors;

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
        return EnumerableTaskCompletionSources.InParallelAsync(_levelsOfParallelism, 
            async taskCompletionSource =>
            {
                await Task.WhenAll(
                    Task.Run(() => ProcessItem(taskCompletionSource)),
                    Task.Delay(_timeSpan, CancellationToken));
            });
    }
}