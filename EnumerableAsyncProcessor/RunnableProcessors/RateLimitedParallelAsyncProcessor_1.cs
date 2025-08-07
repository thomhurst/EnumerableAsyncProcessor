using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.RunnableProcessors.Abstract;
using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class RateLimitedParallelAsyncProcessor<TInput> : AbstractAsyncProcessor<TInput>
{
    private readonly int _levelsOfParallelism;
    
    internal RateLimitedParallelAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task> taskSelector, int levelsOfParallelism, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
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