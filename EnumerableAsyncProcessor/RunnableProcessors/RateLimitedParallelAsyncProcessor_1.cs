using EnumerableAsyncProcessor.RunnableProcessors.Abstract;
using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class RateLimitedParallelAsyncProcessor<TInput> : AbstractAsyncProcessor<TInput>
{
    private readonly int _levelsOfParallelism;
    
    internal RateLimitedParallelAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task> taskSelector, int levelsOfParallelism, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
        ValidationHelper.ThrowIfNegativeOrZero(levelsOfParallelism);

        _levelsOfParallelism = levelsOfParallelism;
    }

    internal override Task Process()
    {
        return WorkerPool.ProcessAsync(TaskWrappers, _levelsOfParallelism, minimumIterationTime: null, CancellationToken);
    }
}
