namespace TomLonghurst.EnumerableAsyncProcessor.Builders;

public class AsyncProcessorBuilder<T>
{
    public static AsyncProcessorBuilderWithItems<T> WithItems(IEnumerable<T> items)
    {
        return new AsyncProcessorBuilderWithItems<T>(items);
    }
}