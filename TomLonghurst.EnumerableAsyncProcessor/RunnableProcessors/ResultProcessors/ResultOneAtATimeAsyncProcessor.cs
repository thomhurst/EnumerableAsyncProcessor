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
            try
            {
                var result = await TaskSelector(itemisedTaskCompletionSourceContainer.Item);
                itemisedTaskCompletionSourceContainer.TaskCompletionSource.SetResult(result);
            }
            catch (Exception e)
            {
                itemisedTaskCompletionSourceContainer.TaskCompletionSource.SetException(e);
            }
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
            try
            {
                var result = await TaskSelector();
                taskCompletionSource.SetResult(result);
            }
            catch (Exception e)
            {
                taskCompletionSource.SetException(e);
            }
        }
    }
}