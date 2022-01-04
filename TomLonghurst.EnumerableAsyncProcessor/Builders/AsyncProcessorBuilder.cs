namespace TomLonghurst.EnumerableAsyncProcessor.Builders;

public static class AsyncProcessorBuilder<T>
{
    public static AsyncProcessorBuilderWithItems<T> WithItems(IEnumerable<T> items)
    {
        return new AsyncProcessorBuilderWithItems<T>(items);
    }
}