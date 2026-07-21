using EnumerableAsyncProcessor.RunnableProcessors.Abstract;
using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class ParallelAsyncProcessor<TInput> : AbstractAsyncProcessor<TInput>
{
    private readonly int? _maxConcurrency;
    private readonly bool _scheduleOnThreadPool;

    internal ParallelAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task> taskSelector, CancellationTokenSource cancellationTokenSource, int? maxConcurrency = null, bool scheduleOnThreadPool = false) : base(items, taskSelector, cancellationTokenSource)
    {
        if (maxConcurrency is { } concurrencyLimit)
        {
            ValidationHelper.ThrowIfNegativeOrZero(concurrencyLimit, nameof(maxConcurrency));
        }

        _maxConcurrency = maxConcurrency;
        _scheduleOnThreadPool = scheduleOnThreadPool;
    }

    internal override async Task Process()
    {
        // If no concurrency limit, process all tasks in parallel
        if (_maxConcurrency == null)
        {
            if (_scheduleOnThreadPool)
            {
                // Use Task.Run to ensure all tasks start immediately on thread pool threads
                // This prevents synchronous code in user delegates from blocking other tasks
                // Small overhead (~1-2μs per task) but necessary for safety with unknown delegates
                await Task.WhenAll(TaskWrappers.Select(taskWrapper => 
                    Task.Run(() => taskWrapper.Process(CancellationToken), CancellationToken)
                )).ConfigureAwait(false);
            }
            else
            {
                // Direct execution for maximum performance when delegates are known to be async
                // WARNING: May cause thread pool starvation if delegates contain blocking code
                await Task.WhenAll(TaskWrappers.Select(taskWrapper => 
                    taskWrapper.Process(CancellationToken)
                )).ConfigureAwait(false);
            }
            return;
        }

        // Throttled processing runs on a fixed worker pool: P worker tasks instead of
        // one queued task, closure and semaphore wait per item
        await WorkerPool.ProcessAsync(TaskWrappers, _maxConcurrency.Value, rateLimiter: null, CancellationToken).ConfigureAwait(false);
    }
}
