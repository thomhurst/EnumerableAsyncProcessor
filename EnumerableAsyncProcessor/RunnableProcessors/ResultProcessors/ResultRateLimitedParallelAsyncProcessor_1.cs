
using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;
using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultRateLimitedParallelAsyncProcessor<TOutput> : ResultAbstractAsyncProcessor<TOutput>
{
    private readonly int _levelsOfParallelism;

    internal ResultRateLimitedParallelAsyncProcessor(int count, Func<Task<TOutput>> taskSelector, int levelsOfParallelism, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
        ValidationHelper.ValidateParallelism(levelsOfParallelism);

        _levelsOfParallelism = levelsOfParallelism;
    }

    internal override Task Process()
    {
        // Task.Run guards the shared worker slots against synchronous code in user delegates
        return TaskWrappers.InParallelAsync(_levelsOfParallelism,
            taskWrapper => Task.Run(() => taskWrapper.Process(CancellationToken), CancellationToken),
            CancellationToken);
    }
}