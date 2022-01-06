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
            if (CancellationToken.IsCancellationRequested)
            {
                taskCompletionSource.TrySetCanceled(CancellationToken);
                return;
            }
            
            var result = await _taskSelector();
            taskCompletionSource.TrySetResult(result);
        }
        catch (Exception e)
        {
            taskCompletionSource.TrySetException(e);
        }
    }
}