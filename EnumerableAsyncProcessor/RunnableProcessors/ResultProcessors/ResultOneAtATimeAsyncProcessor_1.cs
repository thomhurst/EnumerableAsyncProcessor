using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultOneAtATimeAsyncProcessor<TOutput> : ResultAbstractAsyncProcessor<TOutput>
{
    internal ResultOneAtATimeAsyncProcessor(int count, Func<Task<TOutput>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
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