using TomLonghurst.EnumerableAsyncProcessor.Builders;

namespace TomLonghurst.EnumerableAsyncProcessor.Extensions;

public static class EnumerableExtensions
{
    public static AsyncProcessorBuilderWithItems<T> ToAsyncProcessorBuilder<T>(this IEnumerable<T> items)
    {
        return new AsyncProcessorBuilderWithItems<T>(items);
    }
}