using System.Collections.Immutable;
using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class TimedRateLimitedParallelAsyncProcessor<TInput> : AbstractAsyncProcessor<TInput>
{
    private readonly int _levelsOfParallelism;
    private readonly TimeSpan _timeSpan;

    internal TimedRateLimitedParallelAsyncProcessor(ImmutableList<TInput> items, Func<TInput, Task> taskSelector, int levelsOfParallelism, TimeSpan timeSpan, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
        _levelsOfParallelism = levelsOfParallelism;
        _timeSpan = timeSpan;
    }

    internal override Task Process()
    {
        return ItemisedTaskCompletionSourceContainers.InParallelAsync(_levelsOfParallelism, 
            async taskCompletionSource =>
            {
                await Task.WhenAll(
                    Task.Run(() => ProcessItem(taskCompletionSource)),
                    Task.Delay(_timeSpan, CancellationToken));
            });
    }
}