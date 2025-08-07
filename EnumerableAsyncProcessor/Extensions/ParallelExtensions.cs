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

    private static async Task<TResult> ProcessAsync<TSource, TResult>(
        TSource item,
        Func<TSource, Task<TResult>> taskSelector,
        SemaphoreSlim oneAtATime,
        CancellationToken cancellationToken)
    {
        await oneAtATime.WaitAsync(cancellationToken).ConfigureAwait(false);
        
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await taskSelector(item).ConfigureAwait(false);
        }
        finally
        {
            oneAtATime.Release();
        }
    }
    
    private static async Task ProcessAsync<TSource>(
        TSource item,
        Func<TSource, Task> taskSelector,
        SemaphoreSlim oneAtATime,
        CancellationToken cancellationToken)
    {
        await oneAtATime.WaitAsync(cancellationToken).ConfigureAwait(false);
        
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await taskSelector(item).ConfigureAwait(false);
        }
        finally
        {
            oneAtATime.Release();
        }
    }
}