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
            yield return task.Result;
        }
#else
        var managedTasksList = tasks.ToList();

        // Create a cancellation task that will complete when cancellation is requested
        using var cancellationTcs = new CancellationTokenSource();
        var cancellationTask = Task.Delay(Timeout.Infinite, cancellationTcs.Token);
        
        // Register callback to trigger the cancellation task
        using var registration = cancellationToken.Register(() => cancellationTcs.Cancel());

        while (managedTasksList.Count != 0)
        {
            // Check for cancellation before each iteration
            cancellationToken.ThrowIfCancellationRequested();
            
            // Include the cancellation task in WhenAny
            var allTasks = new List<Task>(managedTasksList.Count + 1);
            allTasks.AddRange(managedTasksList);
            allTasks.Add(cancellationTask);
            
            var finishedTask = await Task.WhenAny(allTasks).ConfigureAwait(false);
            
            // If the cancellation task completed, throw cancellation
            if (finishedTask == cancellationTask)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // This should not happen as cancellation should throw above, but as a safety measure:
                throw new OperationCanceledException(cancellationToken);
            }
            
            // Remove and yield the completed task
            var completedTask = (Task<T>)finishedTask;
            managedTasksList.Remove(completedTask);
            yield return await completedTask.ConfigureAwait(false);
        }
#endif
    }

}