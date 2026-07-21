using EnumerableAsyncProcessor.Builders;

namespace EnumerableAsyncProcessor.Extensions;

public static class EnumerableExtensions
{
    public static ItemAsyncProcessorBuilder<T> ToAsyncProcessorBuilder<T>(this IEnumerable<T> items)
    {
        return new ItemAsyncProcessorBuilder<T>(items);
    }
    
    /// <summary>
    /// Creates an async processor builder that can transform items and return results.
    /// </summary>
    /// <typeparam name="T">The input item type.</typeparam>
    /// <typeparam name="TOutput">The output result type.</typeparam>
    /// <param name="items">The items to process.</param>
    /// <param name="taskSelector">The async transformation function.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A builder that can be configured with processing options like ProcessInParallel().</returns>
    /// <remarks>
    /// The processors created by this builder implement IDisposable/IAsyncDisposable and should be properly disposed.
    /// Use 'await using var processor = items.SelectAsync(...).ProcessInParallel();' for automatic disposal.
    /// </remarks>
    public static ItemActionAsyncProcessorBuilder<T, TOutput> SelectAsync<T, TOutput>(this IEnumerable<T> items, Func<T, Task<TOutput>> taskSelector, CancellationToken cancellationToken = default)
    {
        return items.ToAsyncProcessorBuilder()
            .SelectAsync(taskSelector, cancellationToken);
    }

    /// <summary>
    /// Creates an async processor builder with a transformation that observes processor cancellation.
    /// </summary>
    public static ItemActionAsyncProcessorBuilder<T, TOutput> SelectAsync<T, TOutput>(this IEnumerable<T> items, Func<T, CancellationToken, Task<TOutput>> taskSelector, CancellationToken cancellationToken = default)
    {
        return items.ToAsyncProcessorBuilder()
            .SelectAsync(taskSelector, cancellationToken);
    }
    
    /// <summary>
    /// Creates an async processor builder for operations that don't return results.
    /// </summary>
    /// <typeparam name="T">The input item type.</typeparam>
    /// <param name="items">The items to process.</param>
    /// <param name="taskSelector">The async operation to perform on each item.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A builder that can be configured with processing options like ProcessInParallel().</returns>
    /// <remarks>
    /// The processors created by this builder implement IDisposable/IAsyncDisposable and should be properly disposed.
    /// Use 'await using var processor = items.ForEachAsync(...).ProcessInParallel();' for automatic disposal.
    /// </remarks>
    public static ItemActionAsyncProcessorBuilder<T> ForEachAsync<T>(this IEnumerable<T> items, Func<T, Task> taskSelector, CancellationToken cancellationToken = default)
    {
        return items.ToAsyncProcessorBuilder()
            .ForEachAsync(taskSelector, cancellationToken);
    }

    /// <summary>
    /// Creates an async processor builder with an operation that observes processor cancellation.
    /// </summary>
    public static ItemActionAsyncProcessorBuilder<T> ForEachAsync<T>(this IEnumerable<T> items, Func<T, CancellationToken, Task> taskSelector, CancellationToken cancellationToken = default)
    {
        return items.ToAsyncProcessorBuilder()
            .ForEachAsync(taskSelector, cancellationToken);
    }
    
    /// <summary>
    /// Projects each element to an IAsyncEnumerable and flattens the resulting sequences into one sequence.
    /// </summary>
    public static async IAsyncEnumerable<TOutput> SelectManyAsync<T, TOutput>(
        this IEnumerable<T> items,
        Func<T, IAsyncEnumerable<TOutput>> selector,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
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
        this IEnumerable<T> items,
        Func<T, Task<TOutput[]>> selector,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
        this IEnumerable<T> items,
        Func<T, Task<IEnumerable<TOutput>>> selector,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
        this IEnumerable<T> items,
        Func<T, Task<IAsyncEnumerable<TOutput>>> selector,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var asyncEnum = await selector(item).ConfigureAwait(false);
            await foreach (var result in asyncEnum.WithCancellation(cancellationToken))
            {
                yield return result;
            }
        }
    }
    
    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        this IEnumerable<T> items,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
        await Task.CompletedTask; // Suppress CS1998 warning
    }

    internal static async IAsyncEnumerable<T> ToIAsyncEnumerable<T>(this IEnumerable<Task<T>> tasks, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
#if NET9_0_OR_GREATER
        await foreach (var task in Task.WhenEach(tasks).ConfigureAwait(false).WithCancellation(cancellationToken))
        {
            // Await rather than .Result: the task is already complete, but .Result wraps
            // failures in AggregateException while the net8 fallback path rethrows the
            // original exception. Both paths must surface identical exceptions.
            yield return await task.ConfigureAwait(false);
        }
#else
        // Interleaving via completion-order buckets: each task's continuation claims the next
        // bucket, so streaming N tasks is O(N) rather than the O(N^2) of a WhenAny loop.
        var inputTasks = tasks.ToList();

        var buckets = new TaskCompletionSource<Task<T>>[inputTasks.Count];
        for (var i = 0; i < buckets.Length; i++)
        {
            buckets[i] = new TaskCompletionSource<Task<T>>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        var nextBucketIndex = -1;
        foreach (var task in inputTasks)
        {
            _ = task.ContinueWith(
                completedTask => buckets[Interlocked.Increment(ref nextBucketIndex)].TrySetResult(completedTask),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        foreach (var bucket in buckets)
        {
            var completedTask = await bucket.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            yield return await completedTask.ConfigureAwait(false);
        }
#endif
    }

}
