namespace EnumerableAsyncProcessor.Extensions;

public static class ParallelExtensions
{
    public static Task InParallelAsync<TSource, TResult>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task<TResult>> taskSelector)
    {
        var parallelLock = new SemaphoreSlim(initialCount:levelOfParallelism, maxCount:levelOfParallelism);

        return Task.WhenAll(source.Select(item => ProcessAsync(item, taskSelector, parallelLock)));
    }
    
    public static Task InParallelAsync<TSource>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task> taskSelector)
    {
        var parallelLock = new SemaphoreSlim(initialCount:levelOfParallelism, maxCount:levelOfParallelism);

        return Task.WhenAll(source.Select(item => ProcessAsync(item, taskSelector, parallelLock)));
    }

    private static async Task<TResult> ProcessAsync<TSource, TResult>(
        TSource item,
        Func<TSource, Task<TResult>> taskSelector,
        SemaphoreSlim oneAtATime)
    {
        await oneAtATime.WaitAsync();
        
        try
        {
            return await taskSelector(item);
        }
        finally
        {
            oneAtATime.Release();
        }
    }
    
    private static async Task ProcessAsync<TSource>(
        TSource item,
        Func<TSource, Task> taskSelector,
        SemaphoreSlim oneAtATime)
    {
        await oneAtATime.WaitAsync();
        
        try
        {
            await taskSelector(item);
        }
        finally
        {
            oneAtATime.Release();
        }
    }
}