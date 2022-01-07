using System.Collections.Immutable;
using TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class OneAtATimeAsyncProcessor<TInput> : AbstractAsyncProcessor<TInput>
{
    public OneAtATimeAsyncProcessor(ImmutableList<TInput> items, Func<TInput, Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
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