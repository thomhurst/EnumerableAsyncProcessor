﻿namespace TomLonghurst.EnumerableAsyncProcessor.Builders;

public class ItemAsyncProcessorBuilder<TSource>
{
    private readonly IEnumerable<TSource> _items;

    internal ItemAsyncProcessorBuilder(IEnumerable<TSource> items)
    {
        _items = items;
    }

    public ItemActionAsyncProcessorBuilder<TSource, TResult> SelectAsync<TResult>(Func<TSource, Task<TResult>> taskSelector, CancellationToken cancellationToken = default)
    {
        return new ItemActionAsyncProcessorBuilder<TSource, TResult>(_items, taskSelector, cancellationToken);
    }
    
    public ItemActionAsyncProcessorBuilder<TSource> ForEachAsync(Func<TSource, Task> taskSelector, CancellationToken cancellationToken = default)
    {
        return new ItemActionAsyncProcessorBuilder<TSource>(_items, taskSelector, cancellationToken);
    }
}