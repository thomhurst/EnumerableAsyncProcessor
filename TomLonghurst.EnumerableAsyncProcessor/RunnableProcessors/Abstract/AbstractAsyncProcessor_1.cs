using System.Collections.Immutable;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.Abstract;

public abstract class AbstractAsyncProcessor<TInput> : AbstractAsyncProcessorBase
{
    protected readonly IEnumerable<Tuple<TInput, TaskCompletionSource>> ItemisedTaskCompletionSourceContainers;
    
    private readonly Func<TInput, Task> _taskSelector;

    protected AbstractAsyncProcessor(ImmutableList<TInput> items, Func<TInput, Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items.Count, cancellationTokenSource)
    {
        ItemisedTaskCompletionSourceContainers = items.Select((item, index) =>
            new Tuple<TInput, TaskCompletionSource>(item, EnumerableTaskCompletionSources[index]));
        _taskSelector = taskSelector;
    }
    
    protected async Task ProcessItem(Tuple<TInput, TaskCompletionSource> itemisedTaskCompletionSourceContainer)
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