using EnumerableAsyncProcessor.Builders;

namespace EnumerableAsyncProcessor.Extensions;

public static class AsyncEnumerableExtensions
{
    public static AsyncEnumerableAsyncProcessorBuilder<T> ToAsyncProcessorBuilder<T>(this IAsyncEnumerable<T> items)
    {
        return new AsyncEnumerableAsyncProcessorBuilder<T>(items);
    }
    
    public static AsyncEnumerableActionAsyncProcessorBuilder<T, TOutput> SelectAsync<T, TOutput>(
        this IAsyncEnumerable<T> items, 
        Func<T, Task<TOutput>> taskSelector, 
        CancellationToken cancellationToken = default)
    {
        return items.ToAsyncProcessorBuilder()
            .SelectAsync(taskSelector, cancellationToken);
    }
    
    public static AsyncEnumerableActionAsyncProcessorBuilder<T> ForEachAsync<T>(
        this IAsyncEnumerable<T> items, 
        Func<T, Task> taskSelector, 
        CancellationToken cancellationToken = default)
    {
        return items.ToAsyncProcessorBuilder()
            .ForEachAsync(taskSelector, cancellationToken);
    }
    
    /// <summary>
    /// Process items in parallel and return all results as IEnumerable when awaited.
    /// </summary>
    public static async Task<IEnumerable<T>> ProcessInParallel<T>(
        this IAsyncEnumerable<T> items,
        CancellationToken cancellationToken = default)
    {
        return await items.ProcessInParallel(null, false, cancellationToken).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Process items in parallel with specified concurrency and return all results as IEnumerable when awaited.
    /// </summary>
    public static async Task<IEnumerable<T>> ProcessInParallel<T>(
        this IAsyncEnumerable<T> items,
        int maxConcurrency,
        CancellationToken cancellationToken = default)
    {
        return await items.ProcessInParallel((int?)maxConcurrency, false, cancellationToken).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Process items in parallel with optional concurrency and thread pool scheduling, return all results as IEnumerable when awaited.
    /// </summary>
    public static async Task<IEnumerable<T>> ProcessInParallel<T>(
        this IAsyncEnumerable<T> items,
        int? maxConcurrency,
        bool scheduleOnThreadPool,
        CancellationToken cancellationToken = default)
    {
        var results = new List<T>();
        
        if (maxConcurrency.HasValue)
        {
            // Rate-limited parallel processing
            using var semaphore = new SemaphoreSlim(maxConcurrency.Value, maxConcurrency.Value);
            var tasks = new List<Task<T>>();

            await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                
                var capturedItem = item;
                var task = Task.Run(() =>
                {
                    try
                    {
                        return capturedItem;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken);
                
                tasks.Add(task);
            }
            
            if (tasks.Count > 0)
            {
                var taskResults = await Task.WhenAll(tasks).ConfigureAwait(false);
                results.AddRange(taskResults);
            }
        }
        else
        {
            // Unbounded parallel processing
            var tasks = new List<Task<T>>();
            
            await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                var capturedItem = item;
                
                Task<T> task;
                if (scheduleOnThreadPool)
                {
                    task = Task.Run(() => Task.FromResult(capturedItem), cancellationToken);
                }
                else
                {
                    task = Task.FromResult(capturedItem);
                }
                
                tasks.Add(task);
            }
            
            if (tasks.Count > 0)
            {
                var taskResults = await Task.WhenAll(tasks).ConfigureAwait(false);
                results.AddRange(taskResults);
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// Process items in parallel with transformation and return all results as IEnumerable when awaited.
    /// </summary>
    public static async Task<IEnumerable<TOutput>> ProcessInParallel<T, TOutput>(
        this IAsyncEnumerable<T> items,
        Func<T, Task<TOutput>> taskSelector,
        CancellationToken cancellationToken = default)
    {
        return await items.ProcessInParallel(taskSelector, null, false, cancellationToken).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Process items in parallel with transformation and specified concurrency, return all results as IEnumerable when awaited.
    /// </summary>
    public static async Task<IEnumerable<TOutput>> ProcessInParallel<T, TOutput>(
        this IAsyncEnumerable<T> items,
        Func<T, Task<TOutput>> taskSelector,
        int maxConcurrency,
        CancellationToken cancellationToken = default)
    {
        return await items.ProcessInParallel(taskSelector, (int?)maxConcurrency, false, cancellationToken).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Process items in parallel with transformation, optional concurrency and thread pool scheduling, return all results as IEnumerable when awaited.
    /// </summary>
    public static async Task<IEnumerable<TOutput>> ProcessInParallel<T, TOutput>(
        this IAsyncEnumerable<T> items,
        Func<T, Task<TOutput>> taskSelector,
        int? maxConcurrency,
        bool scheduleOnThreadPool,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TOutput>();
        
        if (maxConcurrency.HasValue)
        {
            // Rate-limited parallel processing
            using var semaphore = new SemaphoreSlim(maxConcurrency.Value, maxConcurrency.Value);
            var tasks = new List<Task<TOutput>>();

            await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                
                var capturedItem = item;
                var task = Task.Run(async () =>
                {
                    try
                    {
                        return await taskSelector(capturedItem).ConfigureAwait(false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken);
                
                tasks.Add(task);
            }
            
            if (tasks.Count > 0)
            {
                var taskResults = await Task.WhenAll(tasks).ConfigureAwait(false);
                results.AddRange(taskResults);
            }
        }
        else
        {
            // Unbounded parallel processing
            var tasks = new List<Task<TOutput>>();
            
            await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                var capturedItem = item;
                
                Task<TOutput> task;
                if (scheduleOnThreadPool)
                {
                    task = Task.Run(async () => await taskSelector(capturedItem).ConfigureAwait(false), cancellationToken);
                }
                else
                {
                    task = taskSelector(capturedItem);
                }
                
                tasks.Add(task);
            }
            
            if (tasks.Count > 0)
            {
                var taskResults = await Task.WhenAll(tasks).ConfigureAwait(false);
                results.AddRange(taskResults);
            }
        }
        
        return results;
    }
}