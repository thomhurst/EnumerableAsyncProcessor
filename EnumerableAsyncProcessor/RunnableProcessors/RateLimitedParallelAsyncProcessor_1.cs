using System.Collections.Immutable;
using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class RateLimitedParallelAsyncProcessor<TInput> : AbstractAsyncProcessor<TInput>
{
    private readonly int _levelsOfParallelism;
    
    internal RateLimitedParallelAsyncProcessor(ImmutableList<TInput> items, Func<TInput, Task> taskSelector, int levelsOfParallelism, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
        _levelsOfParallelism = levelsOfParallelism;
    }

    internal override Task Process()
    {
        return ItemisedTaskCompletionSourceContainers.InParallelAsync(_levelsOfParallelism, 
            async taskCompletionSource =>
            {
                await Task.Run(() => ProcessItem(taskCompletionSource));
            });
    }
}