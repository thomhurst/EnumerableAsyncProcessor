using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.RunnableProcessors.Abstract;
using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class RateLimitedParallelAsyncProcessor : AbstractAsyncProcessor
{
    private readonly int _levelsOfParallelism;

    internal RateLimitedParallelAsyncProcessor(int count, Func<Task> taskSelector, int levelsOfParallelism, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
        ValidationHelper.ThrowIfNegativeOrZero(levelsOfParallelism);

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