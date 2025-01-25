using EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class ParallelAsyncProcessor : AbstractAsyncProcessor
{
    internal ParallelAsyncProcessor(int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
    }

    internal override Task Process()
    {
        return Task.WhenAll(TaskWrappers.Select(x => Task.Run(() => ProcessItem(x))));
    }
}