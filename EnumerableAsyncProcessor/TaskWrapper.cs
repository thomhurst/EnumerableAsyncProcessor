namespace EnumerableAsyncProcessor;

public record ActionTaskWrapper(Func<Task> TaskFactory, TaskCompletionSource TaskCompletionSource)
{
    public async Task Process(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource.SetCanceled(cancellationToken);
            return;
        }
        
        try
        {
            await TaskFactory.Invoke();
            TaskCompletionSource.SetResult();
        }
        catch (Exception e)
        {
            TaskCompletionSource.SetException(e);
        }
    }
}

public record ItemTaskWrapper<TInput>(TInput Input, Func<TInput, Task> TaskFactory, TaskCompletionSource TaskCompletionSource)
{
    public async Task Process(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource.SetCanceled(cancellationToken);
            return;
        }
        
        try
        {
            await TaskFactory.Invoke(Input);
            TaskCompletionSource.SetResult();
        }
        catch (Exception e)
        {
            TaskCompletionSource.SetException(e);
        }
    }
}

public record ItemTaskWrapper<TInput, TOutput>(TInput Input, Func<TInput, Task<TOutput>> TaskFactory, TaskCompletionSource<TOutput> TaskCompletionSource)
{
    public async Task Process(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource.SetCanceled(cancellationToken);
            return;
        }
        
        try
        {
            TaskCompletionSource.SetResult(await TaskFactory.Invoke(Input));
        }
        catch (Exception e)
        {
            TaskCompletionSource.SetException(e);
        }
    }
}

public record ActionTaskWrapper<TOutput>(Func<Task<TOutput>> TaskFactory, TaskCompletionSource<TOutput> TaskCompletionSource)
{
    public async Task Process(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource.SetCanceled(cancellationToken);
            return;
        }
        
        try
        { 
            TaskCompletionSource.SetResult(await TaskFactory.Invoke());
        }
        catch (Exception e)
        {
            TaskCompletionSource.SetException(e);
        }
    }
}