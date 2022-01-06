namespace TomLonghurst.EnumerableAsyncProcessor.Builders;

public static class AsyncProcessorBuilder
{
    public static ItemAsyncProcessorBuilder<T> WithItems<T>(IEnumerable<T> items)
    {
        return new ItemAsyncProcessorBuilder<T>(items);
    }

    public static ExecutionCountAsyncProcessorBuilder WithExecutionCount(int count)
    {
        return new ExecutionCountAsyncProcessorBuilder(count);
    }
}