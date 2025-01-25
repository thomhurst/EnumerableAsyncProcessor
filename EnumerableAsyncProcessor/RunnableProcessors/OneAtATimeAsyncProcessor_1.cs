using System.Collections.Immutable;
using EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class OneAtATimeAsyncProcessor<TInput> : AbstractAsyncProcessor<TInput>
{
    internal OneAtATimeAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
    }

    internal override async Task Process()
    {
        foreach (var itemTaskCompletionSourceTuple in TaskWrappers)
        {
            await ProcessItem(itemTaskCompletionSourceTuple);
        }
    }
}