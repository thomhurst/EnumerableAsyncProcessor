namespace TomLonghurst.EnumerableAsyncProcessor.Builders;

public static class AsyncProcessorBuilder<T>
{
    public static ItemAsyncProcessorBuilder<T> WithItems(IEnumerable<T> items)
    {
        return new ItemAsyncProcessorBuilder<T>(items);
    }

    public static ExecutionCountAsyncProcessorBuilder WithExecutionCount(int count)
    {
        return new ExecutionCountAsyncProcessorBuilder(count);
    }
}