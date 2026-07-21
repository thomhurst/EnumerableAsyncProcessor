using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;
using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultTimedRateLimitedParallelAsyncProcessor<TInput, TOutput> : ResultAbstractAsyncProcessor<TInput, TOutput>
{
    private readonly int _levelsOfParallelism;
    private readonly TimeSpan _timeSpan;

    internal ResultTimedRateLimitedParallelAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task<TOutput>> taskSelector, int levelsOfParallelism, TimeSpan timeSpan, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
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
