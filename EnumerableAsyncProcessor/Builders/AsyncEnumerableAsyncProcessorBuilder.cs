#if NET6_0_OR_GREATER
namespace EnumerableAsyncProcessor.Builders;

public class AsyncEnumerableAsyncProcessorBuilder<TInput>
{
    private readonly IAsyncEnumerable<TInput> _items;

    internal AsyncEnumerableAsyncProcessorBuilder(IAsyncEnumerable<TInput> items)
    {
        _items = items;
    }

    public AsyncEnumerableActionAsyncProcessorBuilder<TInput, TOutput> SelectAsync<TOutput>(
        Func<TInput, Task<TOutput>> taskSelector)
    {
        return SelectAsync(taskSelector, CancellationToken.None);
    }

    public AsyncEnumerableActionAsyncProcessorBuilder<TInput, TOutput> SelectAsync<TOutput>(
        Func<TInput, Task<TOutput>> taskSelector, 
        CancellationToken cancellationToken)
    {
        return new AsyncEnumerableActionAsyncProcessorBuilder<TInput, TOutput>(_items, taskSelector, cancellationToken);
    }

    public AsyncEnumerableActionAsyncProcessorBuilder<TInput> ForEachAsync(
        Func<TInput, Task> taskSelector)
    {
        return ForEachAsync(taskSelector, CancellationToken.None);
    }

    public AsyncEnumerableActionAsyncProcessorBuilder<TInput> ForEachAsync(
        Func<TInput, Task> taskSelector, 
        CancellationToken cancellationToken)
    {
        return new AsyncEnumerableActionAsyncProcessorBuilder<TInput>(_items, taskSelector, cancellationToken);
    }
}
#endif