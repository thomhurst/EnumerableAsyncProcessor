using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultRateLimitedParallelAsyncProcessor<TInput, TOutput> : ResultAbstractAsyncProcessor<TInput, TOutput>
{
    private readonly int _levelsOfParallelism;
    
    internal ResultRateLimitedParallelAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task<TOutput>> taskSelector, int levelsOfParallelism, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
        _levelsOfParallelism = levelsOfParallelism;
    }

    internal override Task Process()
    {
        // For rate-limited processing, we want strict parallelism control
        // TaskWrapper.Process already includes Task.Yield to prevent thread pool blocking
        return TaskWrappers.InParallelAsync(_levelsOfParallelism, 
            async taskWrapper =>
            {
                await taskWrapper.Process(CancellationToken).ConfigureAwait(false);
            }, CancellationToken.None);
    }
}