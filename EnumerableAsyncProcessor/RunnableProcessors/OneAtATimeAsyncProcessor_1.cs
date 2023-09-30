using System.Collections.Immutable;
using EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class OneAtATimeAsyncProcessor<TInput> : AbstractAsyncProcessor<TInput>
{
    internal OneAtATimeAsyncProcessor(ImmutableList<TInput> items, Func<TInput, Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
    }

    internal override async Task Process()
    {
        foreach (var itemTaskCompletionSourceTuple in ItemisedTaskCompletionSourceContainers)
        {
            await ProcessItem(itemTaskCompletionSourceTuple);
        }
    }
}