using EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors;

/// <summary>
/// A specialized parallel processor optimized for I/O-bound operations.
/// Allows much higher concurrency levels than CPU-bound processors and removes
/// unnecessary Task.Run wrappers that add overhead for I/O operations.
/// </summary>
public class IOBoundParallelAsyncProcessor : AbstractAsyncProcessor
{
    private readonly int _maxConcurrency;
    
    internal IOBoundParallelAsyncProcessor(int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource, int? maxConcurrency = null) : base(count, taskSelector, cancellationTokenSource)
    {
        // For I/O operations, allow much higher concurrency - default to 10x processor count or minimum 100
        _maxConcurrency = maxConcurrency ?? Math.Max(100, Environment.ProcessorCount * 10);
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