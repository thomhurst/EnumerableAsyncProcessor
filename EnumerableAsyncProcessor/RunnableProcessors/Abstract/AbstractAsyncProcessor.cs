namespace EnumerableAsyncProcessor.RunnableProcessors.Abstract;

public abstract class AbstractAsyncProcessor : AbstractAsyncProcessorBase
{
    private readonly Func<Task> _taskSelector;

    protected AbstractAsyncProcessor(int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, cancellationTokenSource)
    {
        _taskSelector = taskSelector;
    }
    
    protected async Task ProcessItem(TaskCompletionSource taskCompletionSource)
    {
        try
        {
            if (CancellationToken.IsCancellationRequested)
            {
                taskCompletionSource.TrySetCanceled(CancellationToken);
                return;
            }
            
            await _taskSelector();
            taskCompletionSource.TrySetResult();
        }
        catch (Exception e)
        {
            taskCompletionSource.TrySetException(e);
        }
    }
}