using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.RunnableProcessors.Abstract;
using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class RateLimitedParallelAsyncProcessor : AbstractAsyncProcessor
{
    private readonly int _levelsOfParallelism;

    internal RateLimitedParallelAsyncProcessor(int count, Func<Task> taskSelector, int levelsOfParallelism, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
        ValidationHelper.ValidateParallelism(levelsOfParallelism);

        _levelsOfParallelism = levelsOfParallelism;
    }

    internal override Task Process()
    {
        return TaskWrappers.InParallelAsync(_levelsOfParallelism, 
            async taskWrapper =>
            {
                await Task.Run(() => taskWrapper.Process(CancellationToken)).ConfigureAwait(false);
            });
    }
}