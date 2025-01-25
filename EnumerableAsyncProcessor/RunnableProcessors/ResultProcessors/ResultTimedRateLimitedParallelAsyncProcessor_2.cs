using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultTimedRateLimitedParallelAsyncProcessor<TInput, TOutput> : ResultAbstractAsyncProcessor<TInput, TOutput>
{
    private readonly int _levelsOfParallelism;
    private readonly TimeSpan _timeSpan;

    internal ResultTimedRateLimitedParallelAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task<TOutput>> taskSelector, int levelsOfParallelism, TimeSpan timeSpan, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
        _levelsOfParallelism = levelsOfParallelism;
        _timeSpan = timeSpan;
    }

    internal override Task Process()
    {
        return TaskWrappers.InParallelAsync(_levelsOfParallelism, 
            async taskCompletionSource =>
            {
                await Task.WhenAll(
                    Task.Run(() => ProcessItem(taskCompletionSource)),
                    Task.Delay(_timeSpan, CancellationToken));
            });
    }
}