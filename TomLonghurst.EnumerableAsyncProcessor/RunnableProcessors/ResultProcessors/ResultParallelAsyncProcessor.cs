
using System.Collections.Immutable;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultParallelAsyncProcessor<TSource, TResult> : ResultRateLimitedParallelAsyncProcessor<TSource, TResult>
{
    public ResultParallelAsyncProcessor(ImmutableList<TSource> items, Func<TSource, Task<TResult>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, -1, cancellationTokenSource)
    {
    }
}

public class ResultParallelAsyncProcessor<TResult> : ResultRateLimitedParallelAsyncProcessor<TResult>
{
    public ResultParallelAsyncProcessor(int count, Func<Task<TResult>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, -1, cancellationTokenSource)
    {
    }
}