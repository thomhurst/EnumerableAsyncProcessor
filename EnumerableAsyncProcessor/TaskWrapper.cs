namespace EnumerableAsyncProcessor;

public record ActionTaskWrapper(Func<Task> TaskFactory)
{
    public TaskCompletionSource TaskCompletionSource { get; } = new();

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

public record ItemTaskWrapper<TInput>(TInput Input, Func<TInput, Task> TaskFactory)
{
    public TaskCompletionSource TaskCompletionSource { get; } = new();

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

public record ItemTaskWrapper<TInput, TOutput>(TInput Input, Func<TInput, Task<TOutput>> TaskFactory)
{
    public TaskCompletionSource<TOutput> TaskCompletionSource { get; } = new();
    
    public async Task Process(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource.SetCanceled(cancellationToken);
            return;
        }
        
        try
        {
            var result = await TaskFactory.Invoke(Input);
            TaskCompletionSource.SetResult(result);
        }
        catch (Exception e)
        {
            TaskCompletionSource.SetException(e);
        }
    }
}

public record ActionTaskWrapper<TOutput>(Func<Task<TOutput>> TaskFactory)
{
    public TaskCompletionSource<TOutput> TaskCompletionSource { get; } = new();

    public async Task Process(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource.SetCanceled(cancellationToken);
            return;
        }
        
        try
        {
            var result = await TaskFactory.Invoke();
            TaskCompletionSource.SetResult(result);
        }
        catch (Exception e)
        {
            TaskCompletionSource.SetException(e);
        }
    }
}