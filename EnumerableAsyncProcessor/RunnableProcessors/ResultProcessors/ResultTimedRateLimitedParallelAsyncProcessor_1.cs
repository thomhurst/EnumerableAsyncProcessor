using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;
using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultTimedRateLimitedParallelAsyncProcessor<TOutput> : ResultAbstractAsyncProcessor<TOutput>
{
    private readonly int _levelsOfParallelism;
    private readonly TimeSpan _timeSpan;

    internal ResultTimedRateLimitedParallelAsyncProcessor(int count, Func<Task<TOutput>> taskSelector, int levelsOfParallelism, TimeSpan timeSpan, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
        ValidationHelper.ValidateParallelism(levelsOfParallelism);
        ValidationHelper.ValidateTimeSpan(timeSpan);

        _levelsOfParallelism = levelsOfParallelism;
        _timeSpan = timeSpan;
    }

    internal override Task Process()
    {
        // Each worker slot holds an item for at least _timeSpan to honour the rate limit.
        // Task.Run guards the shared worker slots against synchronous code in user delegates
        return TaskWrappers.InParallelAsync(_levelsOfParallelism,
            taskWrapper => Task.WhenAll(
                Task.Run(() => taskWrapper.Process(CancellationToken), CancellationToken),
                Task.Delay(_timeSpan, CancellationToken)),
            CancellationToken);
    }
}