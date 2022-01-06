using System.Collections.Immutable;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.Abstract;

public abstract class AbstractAsyncProcessor<TSource> : AbstractAsyncProcessorBase
{
    protected readonly IEnumerable<Tuple<TSource, TaskCompletionSource>> ItemisedTaskCompletionSourceContainers;
    
    private readonly Func<TSource, Task> _taskSelector;

    protected AbstractAsyncProcessor(ImmutableList<TSource> items, Func<TSource, Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items.Count, cancellationTokenSource)
    {
        ItemisedTaskCompletionSourceContainers = items.Select((item, index) =>
            new Tuple<TSource, TaskCompletionSource>(item, EnumerableTaskCompletionSources[index]));
        _taskSelector = taskSelector;
    }
    
    protected async Task ProcessItem(Tuple<TSource, TaskCompletionSource> itemisedTaskCompletionSourceContainer)
    {
        var (item, taskCompletionSource) = itemisedTaskCompletionSourceContainer;
        try
        {
            if (CancellationToken.IsCancellationRequested)
            {
                taskCompletionSource.TrySetCanceled(CancellationToken);
                return;
            }
            
            await _taskSelector(item);
            taskCompletionSource.TrySetResult();
        }
        catch (Exception e)
        {
            taskCompletionSource.TrySetException(e);
        }
    }
}