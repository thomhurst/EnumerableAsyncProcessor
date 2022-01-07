using System.Collections.Immutable;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class ParallelAsyncProcessor<TInput> : RateLimitedParallelAsyncProcessor<TInput>
{
    public ParallelAsyncProcessor(ImmutableList<TInput> items, Func<TInput, Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, -1, cancellationTokenSource)
    {
    }
}