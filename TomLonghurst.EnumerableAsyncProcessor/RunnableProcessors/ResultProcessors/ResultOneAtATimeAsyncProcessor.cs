namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultOneAtATimeAsyncProcessor<TSource, TResult> : ResultAbstractAsyncProcessor<TSource, TResult>
{
    public ResultOneAtATimeAsyncProcessor(IReadOnlyCollection<TSource> items, Func<TSource, Task<TResult>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
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

public class ResultOneAtATimeAsyncProcessor<TResult> : ResultAbstractAsyncProcessor<TResult>
{
    public ResultOneAtATimeAsyncProcessor(int count, Func<Task<TResult>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
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