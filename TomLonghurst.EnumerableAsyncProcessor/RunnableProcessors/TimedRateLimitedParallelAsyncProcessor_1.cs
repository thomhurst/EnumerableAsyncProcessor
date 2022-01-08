using System.Collections.Immutable;
using TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

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
        return Parallel.ForEachAsync(ItemisedTaskCompletionSourceContainers,
            new ParallelOptions { MaxDegreeOfParallelism = _levelsOfParallelism, CancellationToken = CancellationToken},
            async (itemTaskCompletionSourceTuple, _) =>
            {
                await Task.WhenAll(
                    ProcessItem(itemTaskCompletionSourceTuple),
                    Task.Delay(_timeSpan, CancellationToken)
                );
            });
    }
}