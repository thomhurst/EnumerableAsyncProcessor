namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class OneAtATimeAsyncProcessor<TResult> : AbstractAsyncProcessor<TResult>
{
    private readonly TaskCompletionSource _taskCompletionSource = new();

    public OneAtATimeAsyncProcessor(List<Task<Task<TResult>>> initialTasks, CancellationTokenSource cancellationTokenSource) : base(initialTasks, cancellationTokenSource)
    {
    }

    internal override async Task Process()
    {
        try
        {
            foreach (var task in InitialTasks)
            {
                CancellationToken.ThrowIfCancellationRequested();
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

    public override Task ContinuationTask => _taskCompletionSource.Task;
}

public class OneAtATimeAsyncProcessor : AbstractAsyncProcessor
{
    private readonly TaskCompletionSource _taskCompletionSource = new();

    public OneAtATimeAsyncProcessor(List<Task<Task>> initialTasks, CancellationTokenSource cancellationTokenSource) : base(initialTasks, cancellationTokenSource)
    {
    }

    internal override async Task Process()
    {
        try
        {
            foreach (var task in InitialTasks)
            {
                CancellationToken.ThrowIfCancellationRequested();
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

    public override Task Task => _taskCompletionSource.Task;
}