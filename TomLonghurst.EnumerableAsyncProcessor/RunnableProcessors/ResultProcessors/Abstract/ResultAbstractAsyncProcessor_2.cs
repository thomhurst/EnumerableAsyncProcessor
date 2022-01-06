using System.Runtime.CompilerServices;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

public abstract class ResultAbstractAsyncProcessor<TSource, TResult> : ResultAbstractAsyncProcessorBase<TResult>
{
    protected readonly IEnumerable<Tuple<TSource, TaskCompletionSource<TResult>>> ItemisedTaskCompletionSourceContainers;

    private readonly Func<TSource, Task<TResult>> _taskSelector;

    protected ResultAbstractAsyncProcessor(IReadOnlyCollection<TSource> items, Func<TSource, Task<TResult>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items.Count, cancellationTokenSource)
    {
        ItemisedTaskCompletionSourceContainers = items.Select((item, index) =>
            new Tuple<TSource, TaskCompletionSource<TResult>>(item, EnumerableTaskCompletionSources[index]));
        _taskSelector = taskSelector;
    }
    
    protected async Task ProcessItem(Tuple<TSource, TaskCompletionSource<TResult>> itemisedTaskCompletionSourceContainer)
    {
        var (item, taskCompletionSource) = itemisedTaskCompletionSourceContainer;
        try
        {
            if (CancellationToken.IsCancellationRequested)
            {
                taskCompletionSource.TrySetCanceled(CancellationToken);
                return;
            }
            
            var result = await _taskSelector(item);
            taskCompletionSource.TrySetResult(result);
        }
        catch (Exception e)
        {
            taskCompletionSource.TrySetException(e);
        }
    }
}