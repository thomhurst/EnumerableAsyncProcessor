using EnumerableAsyncProcessor.Interfaces;
using EnumerableAsyncProcessor.RunnableProcessors;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.Builders;

public sealed class ActionAsyncProcessorBuilder
{
    private readonly int _count;
    private readonly Func<Task> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public ActionAsyncProcessorBuilder(int count, Func<Task> taskSelector, CancellationToken cancellationToken)
    {
        _count = count;
        _taskSelector = taskSelector;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    public ActionAsyncProcessorBuilder(int count, Func<CancellationToken, Task> taskSelector, CancellationToken cancellationToken)
    {
        _count = count;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _taskSelector = () => taskSelector(_cancellationTokenSource.Token);
    }

    public IAsyncProcessor ProcessInBatches(int batchSize)
    {
        return new BatchAsyncProcessor(batchSize, _count, _taskSelector, _cancellationTokenSource).StartProcessing();
    }
    
    public IAsyncProcessor ProcessInParallel(int maxConcurrency, TimeSpan timeSpan)
    {
        return ProcessInParallel(maxConcurrency, timeSpan, maxConcurrency);
    }

    /// <summary>Processes tasks with independent start-rate and concurrency limits.</summary>
    /// <param name="permitsPerWindow">Maximum operations that may start in each window.</param>
    /// <param name="window">Rate-limit replenishment window.</param>
    /// <param name="maxConcurrency">Maximum operations that may remain in flight.</param>
    public IAsyncProcessor ProcessInParallel(int permitsPerWindow, TimeSpan window, int maxConcurrency)
    {
        return new TimedRateLimitedParallelAsyncProcessor(_count, _taskSelector, permitsPerWindow, window, maxConcurrency, _cancellationTokenSource).StartProcessing();
    }
    
    /// <summary>
    /// Process tasks in parallel, optionally limiting concurrency.
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations, or null for unbounded concurrency.</param>
    /// <param name="scheduleOnThreadPool">For unbounded processing, schedules tasks on the thread pool when true.</param>
    /// <returns>An async processor configured for parallel execution.</returns>
    public IAsyncProcessor ProcessInParallel(int? maxConcurrency = null, bool scheduleOnThreadPool = false)
    {
        return new ParallelAsyncProcessor(_count, _taskSelector, _cancellationTokenSource, maxConcurrency, scheduleOnThreadPool).StartProcessing();
    }

    /// <summary>
    /// Processes items in parallel with bounded concurrency. Binary-compatible with assemblies
    /// compiled against v3 (equivalent to <c>ProcessInParallel(maxConcurrency: n)</c>).
    /// </summary>
    public IAsyncProcessor ProcessInParallel(int maxConcurrency)
    {
        return ProcessInParallel((int?)maxConcurrency);
    }
    
    public IAsyncProcessor ProcessOneAtATime()
    {
        return new OneAtATimeAsyncProcessor(_count, _taskSelector, _cancellationTokenSource).StartProcessing();
    }

}
