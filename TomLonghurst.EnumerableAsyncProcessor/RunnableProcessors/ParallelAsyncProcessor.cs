using System.Collections.Immutable;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class ParallelAsyncProcessor<TSource> : RateLimitedParallelAsyncProcessor<TSource>
{
    public ParallelAsyncProcessor(ImmutableList<TSource> items, Func<TSource, Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, -1, cancellationTokenSource)
    {
    }
}

public class ParallelAsyncProcessor : RateLimitedParallelAsyncProcessor
{
    public ParallelAsyncProcessor(int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, -1, cancellationTokenSource)
    {
    }
}