#if NET6_0_OR_GREATER
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
    /// Process async enumerable items using a channel-based approach with producer-consumer pattern.
    /// </summary>
    public static IAsyncEnumerableProcessor ForEachWithChannelAsync<T>(
        this IAsyncEnumerable<T> items, 
        Func<T, Task> taskSelector, 
        AsyncEnumerableChannelOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        return items.ToAsyncProcessorBuilder()
            .ForEachAsync(taskSelector, cancellationToken)
            .ProcessWithChannel(options);
    }
    
    /// <summary>
    /// Process async enumerable items using a channel-based approach and return results.
    /// </summary>
    public static IAsyncEnumerableProcessor<TOutput> SelectWithChannelAsync<T, TOutput>(
        this IAsyncEnumerable<T> items, 
        Func<T, Task<TOutput>> taskSelector, 
        AsyncEnumerableChannelOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        return items.ToAsyncProcessorBuilder()
            .SelectAsync(taskSelector, cancellationToken)
            .ProcessWithChannel(options);
    }
}
#endif