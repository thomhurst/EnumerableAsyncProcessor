using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultParallelAsyncProcessor<TInput, TOutput> : ResultAbstractAsyncProcessor<TInput, TOutput>
{
    internal ResultParallelAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task<TOutput>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
    }

    internal override Task Process()
    {
        return Task.WhenAll(TaskWrappers.Select(x => Task.Run(() => ProcessItem(x))));
    }
}