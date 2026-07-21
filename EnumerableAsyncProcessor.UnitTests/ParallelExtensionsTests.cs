using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.UnitTests;

public class ParallelExtensionsTests
{
    [Test]
    [NotInParallel]
    public async Task CpuBoundActionRunsInParallel()
    {
        using var workersStarted = new CountdownEvent(4);
        using var releaseWorkers = new ManualResetEventSlim();

        var processingTask = Enumerable.Range(0, 4).InParallelAsync(
            4,
            (Action<int>)(_ =>
            {
                workersStarted.Signal();
                releaseWorkers.Wait(TimeSpan.FromSeconds(10));
            }));

        var ranInParallel = workersStarted.Wait(TimeSpan.FromSeconds(10));
        releaseWorkers.Set();
        await processingTask;

        await Assert.That(ranInParallel).IsTrue();
    }

    [Test]
    public async Task WorkerPoolDrainsAllItemsAfterFailures()
    {
        var processedItems = new ConcurrentBag<int>();

        var processingTask = Enumerable.Range(0, 20).InParallelAsync(
            3,
            (Func<int, Task>)(item =>
            {
                processedItems.Add(item);

                return item % 4 == 0
                    ? Task.FromException(new InvalidOperationException($"item {item}"))
                    : Task.CompletedTask;
            }));

        try
        {
            await processingTask;
        }
        catch (InvalidOperationException)
        {
            // Expected: awaiting a multiply faulted Task surfaces its first exception.
        }

        await Assert.That(processedItems.Count).IsEqualTo(20);
        await Assert.That(processingTask.Exception!.InnerExceptions.Count).IsEqualTo(5);
    }

    [Test]
    public async Task SourceIsMaterializedOnceBeforeWorkersStart()
    {
        var enumerationCount = 0;

        IEnumerable<int> Source()
        {
            Interlocked.Increment(ref enumerationCount);

            for (var i = 0; i < 10; i++)
            {
                yield return i;
            }
        }

        await Source().InParallelAsync(2, (Func<int, Task>)(_ => Task.CompletedTask));

        await Assert.That(enumerationCount).IsEqualTo(1);
    }
}
