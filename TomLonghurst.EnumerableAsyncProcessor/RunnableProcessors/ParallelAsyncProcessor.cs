namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class ParallelAsyncProcessor : RateLimitedParallelAsyncProcessor
{
    internal ParallelAsyncProcessor(int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, -1, cancellationTokenSource)
    {
    }
}