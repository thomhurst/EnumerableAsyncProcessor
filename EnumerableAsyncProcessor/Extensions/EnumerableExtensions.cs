using EnumerableAsyncProcessor.Builders;
#if NET6_0_OR_GREATER
using EnumerableAsyncProcessor.Interfaces;
#endif

namespace EnumerableAsyncProcessor.Extensions;

public static class EnumerableExtensions
{
    public static ItemAsyncProcessorBuilder<T> ToAsyncProcessorBuilder<T>(this IEnumerable<T> items)
    {
        return new ItemAsyncProcessorBuilder<T>(items);
    }
    
    public static ItemActionAsyncProcessorBuilder<T, TOutput> SelectAsync<T, TOutput>(this IEnumerable<T> items, Func<T, Task<TOutput>> taskSelector, CancellationToken cancellationToken = default)
    {
        return items.ToAsyncProcessorBuilder()
            .SelectAsync(taskSelector, cancellationToken);
    }
    
    public static ItemActionAsyncProcessorBuilder<T> ForEachAsync<T>(this IEnumerable<T> items, Func<T, Task> taskSelector, CancellationToken cancellationToken = default)
    {
        return items.ToAsyncProcessorBuilder()
            .ForEachAsync(taskSelector, cancellationToken);
    }

    internal static async IAsyncEnumerable<T> ToIAsyncEnumerable<T>(this IEnumerable<Task<T>> tasks)
    {
#if NET9_0_OR_GREATER
        await foreach (var task in Task.WhenEach(tasks))
        {
            yield return task.Result;
        }
#else
        var managedTasksList = tasks.ToList();

        while (managedTasksList.Count != 0)
        {
            var finishedTask = await Task.WhenAny(managedTasksList);
            managedTasksList.Remove(finishedTask);
            yield return await finishedTask;
        }
#endif
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Process items using a channel-based approach with producer-consumer pattern.
    /// </summary>
    /// <typeparam name="T">The type of items to process.</typeparam>
    /// <param name="items">The items to process.</param>
    /// <param name="taskSelector">The async task selector function.</param>
    /// <param name="options">Channel configuration options. If null, uses unbounded channel with single consumer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async processor that processes items through a channel.</returns>
    public static IAsyncProcessor ForEachWithChannelAsync<T>(this IEnumerable<T> items, Func<T, Task> taskSelector, ChannelProcessorOptions? options = null, CancellationToken cancellationToken = default)
    {
        return items.ToAsyncProcessorBuilder()
            .ForEachAsync(taskSelector, cancellationToken)
            .ProcessWithChannel(options);
    }
    
    /// <summary>
    /// Process items using a channel-based approach with producer-consumer pattern and return results.
    /// </summary>
    /// <typeparam name="T">The type of input items.</typeparam>
    /// <typeparam name="TOutput">The type of output results.</typeparam>
    /// <param name="items">The items to process.</param>
    /// <param name="taskSelector">The async task selector function.</param>
    /// <param name="options">Channel configuration options. If null, uses unbounded channel with single consumer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async processor that processes items through a channel and returns results.</returns>
    public static IAsyncProcessor<TOutput> SelectWithChannelAsync<T, TOutput>(this IEnumerable<T> items, Func<T, Task<TOutput>> taskSelector, ChannelProcessorOptions? options = null, CancellationToken cancellationToken = default)
    {
        return items.ToAsyncProcessorBuilder()
            .SelectAsync(taskSelector, cancellationToken)
            .ProcessWithChannel(options);
    }
#endif
}