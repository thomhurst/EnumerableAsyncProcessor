namespace EnumerableAsyncProcessor.Extensions;

public static class ParallelExtensions
{
    public static async Task InParallelAsync<TSource, TResult>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task<TResult>> taskSelector)
    {
        if (levelOfParallelism <= 0)
        {
            levelOfParallelism = Environment.ProcessorCount;
        }
        
        using var parallelLock = new SemaphoreSlim(initialCount:levelOfParallelism, maxCount:levelOfParallelism);

        await Task.WhenAll(source.Select(item => ProcessAsync(item, taskSelector, parallelLock)));
    }
    
    public static async Task InParallelAsync<TSource>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task> taskSelector)
    {
        if (levelOfParallelism <= 0)
        {
            levelOfParallelism = Environment.ProcessorCount;
        }
        
        using var parallelLock = new SemaphoreSlim(initialCount:levelOfParallelism, maxCount:levelOfParallelism);

        await Task.WhenAll(source.Select(item => ProcessAsync(item, taskSelector, parallelLock)));
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