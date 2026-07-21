using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;
using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public sealed class ResultTimedRateLimitedParallelAsyncProcessor<TOutput> : ResultAbstractAsyncProcessor<TOutput>
{
    private readonly int _permitsPerWindow;
    private readonly TimeSpan _window;
    private readonly int _maxConcurrency;

    internal ResultTimedRateLimitedParallelAsyncProcessor(int count, Func<Task<TOutput>> taskSelector, int permitsPerWindow, TimeSpan window, int maxConcurrency, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
        ValidationHelper.ThrowIfNegativeOrZero(permitsPerWindow);
        ValidationHelper.ThrowIfNegative(window);
        ValidationHelper.ThrowIfNegativeOrZero(maxConcurrency);

        _permitsPerWindow = permitsPerWindow;
        _window = window;
        _maxConcurrency = maxConcurrency;
    }

    internal override Task Process()
    {
        return WorkerPool.ProcessRateLimitedAsync(TaskWrappers, _maxConcurrency, _permitsPerWindow, _window, CancellationToken);
    }
}
