using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.RunnableProcessors;

namespace EnumerableAsyncProcessor.UnitTests;

/// <summary>
/// Guards the worker-pool execution model used by the bounded parallel and timed
/// rate-limited processors: the concurrency limit must hold, every item must be
/// processed exactly once, oversized limits must not break anything, and cancellation
/// must promptly cancel the unprocessed remainder.
/// </summary>
public class WorkerPoolBehaviourTests
{
    [Test]
    public async Task Positional_And_Named_Concurrency_Limits_Use_The_Same_Processor()
    {
        await using var positional = new[] { 1 }
            .ForEachAsync(_ => Task.CompletedTask)
            .ProcessInParallel(1);
        await using var named = new[] { 1 }
            .ForEachAsync(_ => Task.CompletedTask)
            .ProcessInParallel(maxConcurrency: 1);

        await Assert.That(positional.GetType()).IsEqualTo(typeof(ParallelAsyncProcessor<int>));
        await Assert.That(named.GetType()).IsEqualTo(typeof(ParallelAsyncProcessor<int>));
    }

    [Test, Repeat(3)]
    public async Task MaxConcurrency_Path_Obeys_The_Limit_And_Processes_Everything(CancellationToken cancellationToken)
    {
        const int itemCount = 100;
        const int limit = 8;

        var currentlyRunning = 0;
        var maxObserved = 0;

        await using var processor = Enumerable.Range(0, itemCount).ToList()
            .ForEachAsync(async _ =>
            {
                var running = Interlocked.Increment(ref currentlyRunning);

                int snapshot;
                while (running > (snapshot = Volatile.Read(ref maxObserved)))
                {
                    Interlocked.CompareExchange(ref maxObserved, running, snapshot);
                }

                await Task.Delay(10, cancellationToken);
                Interlocked.Decrement(ref currentlyRunning);
            }, cancellationToken)
            .ProcessInParallel(maxConcurrency: limit);

        await processor.WaitAsync();

        await Assert.That(maxObserved).IsLessThanOrEqualTo(limit);
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.IsCompletedSuccessfully)).IsEqualTo(itemCount);
    }

    [Test]
    public async Task Every_Item_Is_Processed_Exactly_Once_With_Bounded_Concurrency()
    {
        const int itemCount = 500;

        var processedItems = new ConcurrentBag<int>();

        await using var processor = Enumerable.Range(0, itemCount).ToList()
            .ForEachAsync(async i =>
            {
                processedItems.Add(i);
                await Task.Yield();
            })
            .ProcessInParallel(16);

        await processor.WaitAsync();

        await Assert.That(processedItems.Count).IsEqualTo(itemCount);
        await Assert.That(processedItems.Distinct().Count()).IsEqualTo(itemCount);
    }

    [Test]
    public async Task Parallelism_Limit_Larger_Than_Item_Count_Completes_Normally()
    {
        await using var bounded = Enumerable.Range(0, 5).ToList()
            .ForEachAsync(_ => Task.CompletedTask)
            .ProcessInParallel(100);
        await bounded.WaitAsync();

        await using var throttled = Enumerable.Range(0, 5).ToList()
            .SelectAsync(i => Task.FromResult(i))
            .ProcessInParallel(maxConcurrency: 100);
        var results = await throttled.GetResultsAsync();

        await Assert.That(bounded.GetEnumerableTasks().Count(x => x.IsCompletedSuccessfully)).IsEqualTo(5);
        await Assert.That(results.Length).IsEqualTo(5);
    }

    [Test]
    public async Task Timed_RateLimited_Processor_Cancels_Unprocessed_Items_Promptly()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var firstItemStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var processor = Enumerable.Range(0, 20).ToList()
            .ForEachAsync(_ =>
            {
                firstItemStarted.TrySetResult();
                return Task.CompletedTask;
            }, cancellationTokenSource.Token)
            .ProcessInParallel(1, TimeSpan.FromMilliseconds(200));

        await firstItemStarted.Task;
        cancellationTokenSource.Cancel();

        Exception? caught = null;
        try
        {
            await processor.WaitAsync();
        }
        catch (OperationCanceledException exception)
        {
            caught = exception;
        }

        await Assert.That(caught).IsNotNull();
        await Assert.That(processor.GetEnumerableTasks().Any(x => x.IsCanceled)).IsTrue();
        await Assert.That(processor.GetEnumerableTasks().Count(x => !x.IsCompleted)).IsEqualTo(0);

        await processor.DisposeAsync();
    }

    [Test, Timeout(10_000)]
    public async Task Timed_Rate_Limit_Allows_Concurrency_Independent_Of_Permit_Count(CancellationToken cancellationToken)
    {
        const int itemCount = 6;

        var startedCount = 0;
        var allStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var processor = Enumerable.Range(0, itemCount).ToList()
            .ForEachAsync(async _ =>
            {
                if (Interlocked.Increment(ref startedCount) == itemCount)
                {
                    allStarted.TrySetResult();
                }

                await release.Task;
            }, cancellationToken)
            .ProcessInParallel(
                permitsPerWindow: 2,
                window: TimeSpan.FromMilliseconds(100),
                maxConcurrency: itemCount);

        try
        {
            await allStarted.Task.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
        }
        finally
        {
            release.TrySetResult();
        }

        await processor.WaitAsync();

        await Assert.That(startedCount).IsEqualTo(itemCount);
    }

    [Test, Retry(3), Timeout(10_000)]
    public async Task Timed_Rate_Limit_Does_Not_Exceed_Permits_At_Replenishment(CancellationToken cancellationToken)
    {
        const int permitsPerWindow = 3;
        var window = TimeSpan.FromMilliseconds(200);
        var startedAt = new ConcurrentBag<TimeSpan>();
        var stopwatch = Stopwatch.StartNew();

        await using var processor = Enumerable.Range(0, 9).ToList()
            .ForEachAsync(_ =>
            {
                startedAt.Add(stopwatch.Elapsed);
                return Task.CompletedTask;
            }, cancellationToken)
            .ProcessInParallel(permitsPerWindow, window, maxConcurrency: 9);

        await processor.WaitAsync();

        var orderedStarts = startedAt.OrderBy(x => x).ToArray();

        for (var i = permitsPerWindow; i < orderedStarts.Length; i++)
        {
            await Assert.That(orderedStarts[i] - orderedStarts[i - permitsPerWindow])
                .IsGreaterThan(window / 2);
        }
    }

    [Test]
    public async Task Result_Order_Is_Preserved_Regardless_Of_Completion_Order()
    {
        await using var processor = Enumerable.Range(0, 50).ToList()
            .SelectAsync(async i =>
            {
                // Later items complete sooner
                await Task.Delay(50 - i);
                return i;
            })
            .ProcessInParallel(maxConcurrency: 50);

        var results = await processor.GetResultsAsync();

        await Assert.That(results.SequenceEqual(Enumerable.Range(0, 50))).IsTrue();
    }
}
