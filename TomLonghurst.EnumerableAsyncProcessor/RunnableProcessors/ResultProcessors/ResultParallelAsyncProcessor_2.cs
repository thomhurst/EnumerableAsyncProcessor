using System.Collections.Immutable;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultParallelAsyncProcessor<TInput, TOutput> : ResultRateLimitedParallelAsyncProcessor<TInput, TOutput>
{
    public ResultParallelAsyncProcessor(ImmutableList<TInput> items, Func<TInput, Task<TOutput>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, -1, cancellationTokenSource)
    {
    }
}