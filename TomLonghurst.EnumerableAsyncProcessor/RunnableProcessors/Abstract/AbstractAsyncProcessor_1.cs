using System.Collections.Immutable;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.Abstract;

public abstract class AbstractAsyncProcessor<TSource> : AbstractAsyncProcessorBase
{
    protected readonly IEnumerable<ItemisedTaskCompletionSourceContainer<TSource>> ItemisedTaskCompletionSourceContainers;
    
    private readonly Func<TSource, Task> _taskSelector;

    protected AbstractAsyncProcessor(ImmutableList<TSource> items, Func<TSource, Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items.Count, cancellationTokenSource)
    {
        ItemisedTaskCompletionSourceContainers = items.Select((item, index) =>
            new ItemisedTaskCompletionSourceContainer<TSource>(item, EnumerableTaskCompletionSources[index]));
        _taskSelector = taskSelector;
    }
    
    protected async Task ProcessItem(ItemisedTaskCompletionSourceContainer<TSource> itemisedTaskCompletionSourceContainer)
    {
        try
        {
            if (CancellationToken.IsCancellationRequested)
            {
                itemisedTaskCompletionSourceContainer.TaskCompletionSource.TrySetCanceled(CancellationToken);
                return;
            }
            
            await _taskSelector(itemisedTaskCompletionSourceContainer.Item);
            itemisedTaskCompletionSourceContainer.TaskCompletionSource.TrySetResult();
        }
        catch (Exception e)
        {
            itemisedTaskCompletionSourceContainer.TaskCompletionSource.TrySetException(e);
        }
    }
}