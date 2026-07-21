namespace EnumerableAsyncProcessor;

/// <summary>
/// Runs task wrappers on a fixed pool of worker loops instead of queueing one throttled task per item.
/// For N items and P workers this costs P Task.Run tasks and one Interlocked increment per item,
/// rather than N Task.Run tasks, N closures and N semaphore waits.
/// </summary>
internal static class WorkerPool
{
    /// <param name="minimumIterationTime">
    /// When set, each worker holds its slot for at least this long per item, which caps throughput
    /// at (workerCount / minimumIterationTime) operations for the timed rate-limited processors.
    /// </param>
    internal static Task ProcessAsync<TWrapper>(
        TWrapper[] taskWrappers,
        int workerCount,
        TimeSpan? minimumIterationTime,
        CancellationToken cancellationToken) where TWrapper : ITaskWrapper
    {
        workerCount = Math.Min(workerCount, taskWrappers.Length);

        if (workerCount == 0)
        {
            return Task.CompletedTask;
        }

        var nextIndex = -1;

        var workers = new Task[workerCount];

        for (var i = 0; i < workerCount; i++)
        {
            // Task.Run guards the worker slots against synchronous code in user delegates
            workers[i] = Task.Run(async () =>
            {
                while (true)
                {
                    var index = Interlocked.Increment(ref nextIndex);

                    if (index >= taskWrappers.Length)
                    {
                        return;
                    }

                    // Process never throws; it completes the item's TaskCompletionSource instead,
                    // so one failed item cannot stop the worker from draining the rest.
                    var processTask = taskWrappers[index].Process(cancellationToken);

                    if (minimumIterationTime is { } minimumTime)
                    {
                        await Task.WhenAll(processTask, Task.Delay(minimumTime, cancellationToken)).ConfigureAwait(false);
                    }
                    else
                    {
                        await processTask.ConfigureAwait(false);
                    }
                }
            }, cancellationToken);
        }

        return Task.WhenAll(workers);
    }
}
