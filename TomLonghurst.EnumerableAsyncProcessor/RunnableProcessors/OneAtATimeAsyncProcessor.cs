namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class OneAtATimeAsyncProcessor<TResult> : AbstractAsyncProcessor<TResult>
{
    private readonly TaskCompletionSource _taskCompletionSource = new();

    public OneAtATimeAsyncProcessor(List<Task<Task<TResult>>> initialTasks, CancellationToken cancellationToken) : base(initialTasks, cancellationToken)
    {
    }

    internal override async Task Process()
    {
        try
        {
            foreach (var task in _initialTasks)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                task.Start();
                await task.Unwrap();
            }

            _taskCompletionSource.TrySetResult();
        }
        catch (Exception e)
        {
            _taskCompletionSource.TrySetException(e);
        }
    }

    public override Task GetOverallProgressTask()
    {
        return _taskCompletionSource.Task;
    }
}