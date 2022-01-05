namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

public abstract class ResultAbstractAsyncProcessor<TResult> : ResultAbstractAsyncProcessorBase<TResult>
{
    private readonly Func<Task<TResult>> _taskSelector;

    protected ResultAbstractAsyncProcessor(int count, Func<Task<TResult>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, cancellationTokenSource)
    {
        _taskSelector = taskSelector;
    }
    
    protected async Task ProcessItem(TaskCompletionSource<TResult> taskCompletionSource)
    {
        try
        {
            CancellationToken.ThrowIfCancellationRequested();
            var result = await _taskSelector();
            taskCompletionSource.SetResult(result);
        }
        catch (Exception e)
        {
            taskCompletionSource.SetException(e);
        }
    }
}