using System.Collections.Immutable;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class OneAtATimeAsyncProcessor<TSource> : AbstractAsyncProcessor<TSource>
{
    public OneAtATimeAsyncProcessor(ImmutableList<TSource> items, Func<TSource, Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
    }

    internal override async Task Process()
    {
        foreach (var itemisedTaskCompletionSourceContainer in ItemisedTaskCompletionSourceContainers)
        {
            try
            {
                await TaskSelector(itemisedTaskCompletionSourceContainer.Item);
                itemisedTaskCompletionSourceContainer.TaskCompletionSource.SetResult();
            }
            catch (Exception e)
            {
                itemisedTaskCompletionSourceContainer.TaskCompletionSource.SetException(e);
            }
        }
    }
}

public class OneAtATimeAsyncProcessor : AbstractAsyncProcessor
{
    public OneAtATimeAsyncProcessor(int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
    }

    internal override async Task Process()
    {
        foreach (var taskCompletionSource in EnumerableTaskCompletionSources)
        {
            try
            {
                await TaskSelector();
                taskCompletionSource.SetResult();
            }
            catch (Exception e)
            {
                taskCompletionSource.SetException(e);
            }
        }
    }
}