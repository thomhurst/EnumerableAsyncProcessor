using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

/// <summary>
/// A specialized parallel result processor optimized for I/O-bound operations.
/// Allows much higher concurrency levels than CPU-bound processors and removes
/// unnecessary Task.Run wrappers that add overhead for I/O operations.
/// </summary>
public class ResultIOBoundParallelAsyncProcessor<TOutput> : ResultAbstractAsyncProcessor<TOutput>
{
    private readonly int _maxConcurrency;
    
    internal ResultIOBoundParallelAsyncProcessor(int count, Func<Task<TOutput>> taskSelector, CancellationTokenSource cancellationTokenSource, int? maxConcurrency = null) : base(count, taskSelector, cancellationTokenSource)
    {
        // For I/O operations, allow much higher concurrency - default to 64x processor count or minimum 1000
        // This enables scenarios like running 10,000 unit tests in parallel
        _maxConcurrency = maxConcurrency ?? Math.Max(1000, Environment.ProcessorCount * 64);
    }

    internal override async Task Process()
    {
        // For high-concurrency I/O operations, use a throttling approach with SemaphoreSlim
        using var semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
        
        var tasks = TaskWrappers.Select(async taskWrapper =>
        {
            await semaphore.WaitAsync(CancellationToken).ConfigureAwait(false);
            try
            {
                var task = taskWrapper.Process(CancellationToken);
                // Fast-path for already completed tasks
                if (task.IsCompleted)
                {
                    return;
                }
                await task.ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}