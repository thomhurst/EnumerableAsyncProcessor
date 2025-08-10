namespace EnumerableAsyncProcessor.Extensions;

public static class ParallelExtensions
{
    public static async Task InParallelAsync<TSource, TResult>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task<TResult>> taskSelector)
    {
        await InParallelAsync(source, levelOfParallelism, taskSelector, CancellationToken.None).ConfigureAwait(false);
    }

    public static async Task InParallelAsync<TSource, TResult>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task<TResult>> taskSelector,
        CancellationToken cancellationToken)
    {
        if (levelOfParallelism <= 0)
        {
            levelOfParallelism = Environment.ProcessorCount;
        }
        
        using var parallelLock = new SemaphoreSlim(initialCount:levelOfParallelism, maxCount:levelOfParallelism);

        await Task.WhenAll(source.Select(item => ProcessAsync(item, taskSelector, parallelLock, cancellationToken))).ConfigureAwait(false);
    }
    
    public static async Task InParallelAsync<TSource>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task> taskSelector)
    {
        await InParallelAsync(source, levelOfParallelism, taskSelector, CancellationToken.None).ConfigureAwait(false);
    }

    public static async Task InParallelAsync<TSource>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task> taskSelector,
        CancellationToken cancellationToken)
    {
        if (levelOfParallelism <= 0)
        {
            levelOfParallelism = Environment.ProcessorCount;
        }
        
        using var parallelLock = new SemaphoreSlim(initialCount:levelOfParallelism, maxCount:levelOfParallelism);

        await Task.WhenAll(source.Select(item => ProcessAsync(item, taskSelector, parallelLock, cancellationToken))).ConfigureAwait(false);
    }

    // Overloads for CPU-bound processing
    public static async Task InParallelAsync<TSource, TResult>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, TResult> taskSelector,
        CancellationToken cancellationToken = default)
    {
        await InParallelAsync(source, levelOfParallelism, item => Task.FromResult(taskSelector(item)), cancellationToken).ConfigureAwait(false);
    }

    public static async Task InParallelAsync<TSource>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Action<TSource> taskSelector,
        CancellationToken cancellationToken = default)
    {
        await InParallelAsync(source, levelOfParallelism, item => { taskSelector(item); return Task.CompletedTask; }, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<TResult> ProcessAsync<TSource, TResult>(
        TSource item,
        Func<TSource, Task<TResult>> taskSelector,
        SemaphoreSlim parallelLock,
        CancellationToken cancellationToken)
    {
        var semaphoreAcquired = false;
        
        try
        {
            await parallelLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            semaphoreAcquired = true;
            
            cancellationToken.ThrowIfCancellationRequested();
            var task = taskSelector(item);
            
            // Fast-path optimization for already completed tasks
            if (task.IsCompleted)
            {
                if (task.IsFaulted)
                    throw task.Exception?.GetBaseException() ?? task.Exception!;
                if (task.IsCanceled)
                    throw new OperationCanceledException();
                return task.Result;
            }

            // Await the task directly - it's already async so no need for Task.Run
            return await task.ConfigureAwait(false);
        }
        finally
        {
            if (semaphoreAcquired)
            {
                parallelLock.Release();
            }
        }
    }
    
    private static async Task ProcessAsync<TSource>(
        TSource item,
        Func<TSource, Task> taskSelector,
        SemaphoreSlim parallelLock,
        CancellationToken cancellationToken)
    {
        var semaphoreAcquired = false;
        
        try
        {
            await parallelLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            semaphoreAcquired = true;
            
            cancellationToken.ThrowIfCancellationRequested();
            var task = taskSelector(item);
            
            // Fast-path optimization for already completed tasks
            if (task.IsCompleted)
            {
                if (task.IsFaulted)
                    throw task.Exception?.GetBaseException() ?? task.Exception!;
                if (task.IsCanceled)
                    throw new OperationCanceledException();
                return;
            }

            // Await the task directly - it's already async so no need for Task.Run
            await task.ConfigureAwait(false);
        }
        finally
        {
            if (semaphoreAcquired)
            {
                parallelLock.Release();
            }
        }
    }
}