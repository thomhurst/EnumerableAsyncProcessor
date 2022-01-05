namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.Abstract;

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
            CancellationToken.ThrowIfCancellationRequested();
            await _taskSelector();
            taskCompletionSource.SetResult();
        }
        catch (Exception e)
        {
            taskCompletionSource.SetException(e);
        }
    }
}