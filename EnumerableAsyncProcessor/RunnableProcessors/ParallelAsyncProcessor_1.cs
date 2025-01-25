using System.Collections.Immutable;
using EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class ParallelAsyncProcessor<TInput> : AbstractAsyncProcessor<TInput>
{
    internal ParallelAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
    }

    internal override Task Process()
    {
        return Task.WhenAll(TaskWrappers.Select(x => Task.Run(() => ProcessItem(x))));
    }
}