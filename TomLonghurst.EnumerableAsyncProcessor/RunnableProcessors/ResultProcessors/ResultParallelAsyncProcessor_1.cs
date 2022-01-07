namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultParallelAsyncProcessor<TOutput> : ResultRateLimitedParallelAsyncProcessor<TOutput>
{
    public ResultParallelAsyncProcessor(int count, Func<Task<TOutput>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, -1, cancellationTokenSource)
    {
    }
}