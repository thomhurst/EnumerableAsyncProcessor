using System.Collections.Immutable;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class OneAtATimeAsyncProcessor<TSource> : AbstractAsyncProcessor<TSource>
{
    public OneAtATimeAsyncProcessor(ImmutableList<TSource> items, Func<TSource, Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
    }

    internal override async Task Process()
    {
        foreach (var itemisedTaskCompletionSourceContainer in ItemisedTaskCompletionSourceContainers)
        {
            await ProcessItem(itemisedTaskCompletionSourceContainer);
        }
    }
}

public class OneAtATimeAsyncProcessor : AbstractAsyncProcessor
{
    public OneAtATimeAsyncProcessor(int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
    }

    internal override async Task Process()
    {
        foreach (var taskCompletionSource in EnumerableTaskCompletionSources)
        {
            await ProcessItem(taskCompletionSource);
        }
    }
}