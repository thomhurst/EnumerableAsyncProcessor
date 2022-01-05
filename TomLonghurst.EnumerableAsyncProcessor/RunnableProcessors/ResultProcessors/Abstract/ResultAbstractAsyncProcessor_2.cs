namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

public abstract class ResultAbstractAsyncProcessor<TSource, TResult> : ResultAbstractAsyncProcessorBase<TResult>
{
    protected readonly IEnumerable<ItemisedTaskCompletionSourceContainer<TSource, TResult>> ItemisedTaskCompletionSourceContainers;

    private readonly Func<TSource, Task<TResult>> _taskSelector;

    protected ResultAbstractAsyncProcessor(IReadOnlyCollection<TSource> items, Func<TSource, Task<TResult>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items.Count, cancellationTokenSource)
    {
        ItemisedTaskCompletionSourceContainers = items.Select((item, index) =>
            new ItemisedTaskCompletionSourceContainer<TSource, TResult>(item, EnumerableTaskCompletionSources[index]));
        _taskSelector = taskSelector;
    }
    
    protected async Task ProcessItem(ItemisedTaskCompletionSourceContainer<TSource, TResult> itemisedTaskCompletionSourceContainer)
    {
        try
        {
            CancellationToken.ThrowIfCancellationRequested();
            var result = await _taskSelector(itemisedTaskCompletionSourceContainer.Item);
            itemisedTaskCompletionSourceContainer.TaskCompletionSource.SetResult(result);
        }
        catch (Exception e)
        {
            itemisedTaskCompletionSourceContainer.TaskCompletionSource.SetException(e);
        }
    }
}