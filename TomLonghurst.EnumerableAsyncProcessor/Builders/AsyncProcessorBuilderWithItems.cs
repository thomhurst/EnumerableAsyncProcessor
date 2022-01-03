namespace TomLonghurst.EnumerableAsyncProcessor.Builders;

public class AsyncProcessorBuilderWithItems<TSource>
{
    private readonly IEnumerable<TSource> _items;

    internal AsyncProcessorBuilderWithItems(IEnumerable<TSource> items)
    {
        _items = items;
    }

    public AsyncProcessorBuilderWithAction<TSource, TResult> WithAction<TResult>(Func<TSource, Task<TResult>> taskSelector)
    {
        return new AsyncProcessorBuilderWithAction<TSource, TResult>(_items, taskSelector);
    }
}