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

    public static AsyncEnumerableActionAsyncProcessorBuilder<T, TOutput> SelectAsync<T, TOutput>(
        this IAsyncEnumerable<T> items,
        Func<T, CancellationToken, Task<TOutput>> taskSelector,
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

    public static AsyncEnumerableActionAsyncProcessorBuilder<T> ForEachAsync<T>(
        this IAsyncEnumerable<T> items,
        Func<T, CancellationToken, Task> taskSelector,
        CancellationToken cancellationToken = default)
    {
        return items.ToAsyncProcessorBuilder()
            .ForEachAsync(taskSelector, cancellationToken);
    }
    
    /// <summary>
    /// Projects each element to an array and flattens the resulting sequences into one sequence.
    /// </summary>
    public static async IAsyncEnumerable<TOutput> SelectMany<T, TOutput>(
        this IAsyncEnumerable<T> items,
        Func<T, TOutput[]> selector,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in items.WithCancellation(cancellationToken))
        {
            foreach (var result in selector(item))
            {
                yield return result;
            }
        }
    }
    
    /// <summary>
    /// Projects each element to an IEnumerable and flattens the resulting sequences into one sequence.
    /// </summary>
    public static async IAsyncEnumerable<TOutput> SelectMany<T, TOutput>(
        this IAsyncEnumerable<T> items,
        Func<T, IEnumerable<TOutput>> selector,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in items.WithCancellation(cancellationToken))
        {
            foreach (var result in selector(item))
            {
                yield return result;
            }
        }
    }
    
    /// <summary>
    /// Projects each element to an IAsyncEnumerable and flattens the resulting sequences into one sequence.
    /// </summary>
    public static async IAsyncEnumerable<TOutput> SelectMany<T, TOutput>(
        this IAsyncEnumerable<T> items,
        Func<T, IAsyncEnumerable<TOutput>> selector,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in items.WithCancellation(cancellationToken))
        {
            await foreach (var result in selector(item).WithCancellation(cancellationToken))
            {
                yield return result;
            }
        }
    }
    
    /// <summary>
    /// Projects each element to an array asynchronously and flattens the resulting sequences into one sequence.
    /// </summary>
    public static async IAsyncEnumerable<TOutput> SelectManyAsync<T, TOutput>(
        this IAsyncEnumerable<T> items,
        Func<T, Task<TOutput[]>> selector,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in items.WithCancellation(cancellationToken))
        {
            var results = await selector(item).ConfigureAwait(false);
            foreach (var result in results)
            {
                yield return result;
            }
        }
    }
    
    /// <summary>
    /// Projects each element to an IEnumerable asynchronously and flattens the resulting sequences into one sequence.
    /// </summary>
    public static async IAsyncEnumerable<TOutput> SelectManyAsync<T, TOutput>(
        this IAsyncEnumerable<T> items,
        Func<T, Task<IEnumerable<TOutput>>> selector,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in items.WithCancellation(cancellationToken))
        {
            var results = await selector(item).ConfigureAwait(false);
            foreach (var result in results)
            {
                yield return result;
            }
        }
    }
    
    /// <summary>
    /// Projects each element to an IAsyncEnumerable asynchronously and flattens the resulting sequences into one sequence.
    /// </summary>
    public static async IAsyncEnumerable<TOutput> SelectManyAsync<T, TOutput>(
        this IAsyncEnumerable<T> items,
        Func<T, Task<IAsyncEnumerable<TOutput>>> selector,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in items.WithCancellation(cancellationToken))
        {
            var asyncEnum = await selector(item).ConfigureAwait(false);
            await foreach (var result in asyncEnum.WithCancellation(cancellationToken))
            {
                yield return result;
            }
        }
    }
    
    /// <summary>
    /// Collects every item from the source into a list. This overload has no work to parallelize;
    /// it is kept for binary compatibility with assemblies compiled against v3 (notably TUnit).
    /// </summary>
    [Obsolete("This overload only collects the stream and parallelizes nothing. Pass a selector, or enumerate the stream directly. It exists for binary compatibility with assemblies compiled against v3.")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static async Task<IEnumerable<T>> ProcessInParallel<T>(
        this IAsyncEnumerable<T> items,
        CancellationToken cancellationToken = default)
    {
        var results = new List<T>();

        await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            results.Add(item);
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
            Func<T, Task<TOutput>> effectiveSelector = scheduleOnThreadPool
                ? item => Task.Run(() => taskSelector(item), cancellationToken)
                : taskSelector;

            await foreach (var result in AsyncEnumerableWorkerPool.ProcessResultsAsync(
                               items,
                               effectiveSelector,
                               maxConcurrency.Value,
                               cancellationToken).ConfigureAwait(false))
            {
                results.Add(result);
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
                    task = Task.Run(() => taskSelector(capturedItem), cancellationToken);
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
