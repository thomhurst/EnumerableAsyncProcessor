namespace TomLonghurst.EnumerableAsyncProcessor.Builders;

public class AsyncProcessorBuilderWithItems<TSource>
{
    private readonly IEnumerable<TSource> _items;

    internal AsyncProcessorBuilderWithItems(IEnumerable<TSource> items)
    {
        _items = items;
    }

    public AsyncProcessorBuilderWithAction<TSource, TResult> SelectAsync<TResult>(Func<TSource, Task<TResult>> taskSelector)
    {
        return new AsyncProcessorBuilderWithAction<TSource, TResult>(_items, taskSelector);
    }
}