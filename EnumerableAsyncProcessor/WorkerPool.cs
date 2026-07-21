using System.Threading.RateLimiting;

namespace EnumerableAsyncProcessor;

/// <summary>
/// Runs task wrappers on a fixed pool of worker loops instead of queueing one throttled task per item.
/// For N items and P workers this costs P Task.Run tasks and one Interlocked increment per item,
/// rather than N Task.Run tasks, N closures and N semaphore waits.
/// </summary>
internal static class WorkerPool
{
    internal static Task ProcessAsync<TWrapper>(
        TWrapper[] taskWrappers,
        int workerCount,
        RateLimiter? rateLimiter,
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

                    if (rateLimiter is not null)
                    {
                        using var lease = await rateLimiter.AcquireAsync(1, cancellationToken).ConfigureAwait(false);

                        if (!lease.IsAcquired)
                        {
                            throw new InvalidOperationException("The rate limiter could not acquire a permit.");
                        }
                    }

                    // Process never throws; it completes the item's TaskCompletionSource instead,
                    // so one failed item cannot stop the worker from draining the rest.
                    await taskWrappers[index].Process(cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        return Task.WhenAll(workers);
    }

    internal static async Task ProcessRateLimitedAsync<TWrapper>(
        TWrapper[] taskWrappers,
        int workerCount,
        int permitsPerWindow,
        TimeSpan window,
        CancellationToken cancellationToken) where TWrapper : ITaskWrapper
    {
        if (window == TimeSpan.Zero)
        {
            await ProcessAsync(taskWrappers, workerCount, rateLimiter: null, cancellationToken).ConfigureAwait(false);
            return;
        }

        using var rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = permitsPerWindow,
            TokensPerPeriod = permitsPerWindow,
            ReplenishmentPeriod = window,
            AutoReplenishment = true,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = workerCount
        });

        await ProcessAsync(taskWrappers, workerCount, rateLimiter, cancellationToken).ConfigureAwait(false);
    }
}
