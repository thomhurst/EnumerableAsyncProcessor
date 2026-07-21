using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;
using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultTimedRateLimitedParallelAsyncProcessor<TOutput> : ResultAbstractAsyncProcessor<TOutput>
{
    private readonly int _levelsOfParallelism;
    private readonly TimeSpan _timeSpan;

    internal ResultTimedRateLimitedParallelAsyncProcessor(int count, Func<Task<TOutput>> taskSelector, int levelsOfParallelism, TimeSpan timeSpan, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
        ValidationHelper.ThrowIfNegativeOrZero(levelsOfParallelism);
        ValidationHelper.ThrowIfNegative(timeSpan);

        _levelsOfParallelism = levelsOfParallelism;
        _timeSpan = timeSpan;
    }

    internal override Task Process()
    {
        return WorkerPool.ProcessAsync(TaskWrappers, _levelsOfParallelism, minimumIterationTime: _timeSpan, CancellationToken);
    }
}
