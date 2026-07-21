namespace EnumerableAsyncProcessor.Builders;

public sealed class ItemAsyncProcessorBuilder<TInput>
{
    private readonly IEnumerable<TInput> _items;

    internal ItemAsyncProcessorBuilder(IEnumerable<TInput> items)
    {
        _items = items;
    }

    public ItemActionAsyncProcessorBuilder<TInput, TOutput> SelectAsync<TOutput>(Func<TInput, Task<TOutput>> taskSelector)
    {
        return SelectAsync(taskSelector, CancellationToken.None);
    }

    public ItemActionAsyncProcessorBuilder<TInput, TOutput> SelectAsync<TOutput>(Func<TInput, Task<TOutput>> taskSelector, CancellationToken cancellationToken)
    {
        return new ItemActionAsyncProcessorBuilder<TInput, TOutput>(_items, taskSelector, cancellationToken);
    }

    public ItemActionAsyncProcessorBuilder<TInput, TOutput> SelectAsync<TOutput>(Func<TInput, CancellationToken, Task<TOutput>> taskSelector)
    {
        return SelectAsync(taskSelector, CancellationToken.None);
    }

    public ItemActionAsyncProcessorBuilder<TInput, TOutput> SelectAsync<TOutput>(Func<TInput, CancellationToken, Task<TOutput>> taskSelector, CancellationToken cancellationToken)
    {
        return new ItemActionAsyncProcessorBuilder<TInput, TOutput>(_items, taskSelector, cancellationToken);
    }

    public ItemActionAsyncProcessorBuilder<TInput> ForEachAsync(Func<TInput, Task> taskSelector)
    {
        return ForEachAsync(taskSelector, CancellationToken.None);
    }

    public ItemActionAsyncProcessorBuilder<TInput> ForEachAsync(Func<TInput, Task> taskSelector, CancellationToken cancellationToken)
    {
        return new ItemActionAsyncProcessorBuilder<TInput>(_items, taskSelector, cancellationToken);
    }

    public ItemActionAsyncProcessorBuilder<TInput> ForEachAsync(Func<TInput, CancellationToken, Task> taskSelector)
    {
        return ForEachAsync(taskSelector, CancellationToken.None);
    }

    public ItemActionAsyncProcessorBuilder<TInput> ForEachAsync(Func<TInput, CancellationToken, Task> taskSelector, CancellationToken cancellationToken)
    {
        return new ItemActionAsyncProcessorBuilder<TInput>(_items, taskSelector, cancellationToken);
    }
}
