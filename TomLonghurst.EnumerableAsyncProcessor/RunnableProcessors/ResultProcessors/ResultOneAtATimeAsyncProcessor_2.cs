using TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultOneAtATimeAsyncProcessor<TInput, TOutput> : ResultAbstractAsyncProcessor<TInput, TOutput>
{
    public ResultOneAtATimeAsyncProcessor(IReadOnlyCollection<TInput> items, Func<TInput, Task<TOutput>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
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