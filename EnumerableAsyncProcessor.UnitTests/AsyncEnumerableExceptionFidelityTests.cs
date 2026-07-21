using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;
using TUnit.Assertions;
using TUnit.Core;

namespace EnumerableAsyncProcessor.UnitTests;

/// <summary>
/// Guards exception fidelity and abandonment semantics on the IAsyncEnumerable paths:
/// - the Task from void ExecuteAsync must carry every failure via Task.Exception.InnerExceptions,
///   matching the IEnumerable processors (issue #362),
/// - a mid-enumeration source failure must stay the primary exception instead of being masked
///   by an in-flight task failure (issue #363),
/// - breaking out of an unbounded result stream must cancel in-flight work instead of silently
///   blocking until it finishes naturally (issue #363),
/// - the unbounded ProcessInParallel extension must drain started tasks on mid-enumeration
///   failure instead of abandoning them fire-and-forget (issue #364).
/// </summary>
public class AsyncEnumerableExceptionFidelityTests
{
    private static async IAsyncEnumerable<int> Source(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }

    private static async IAsyncEnumerable<int> ThrowingSource(int yieldBeforeThrow)
    {
        for (var i = 0; i < yieldBeforeThrow; i++)
        {
            await Task.Yield();
            yield return i;
        }

        throw new InvalidOperationException("source failed");
    }

    [Test]
    public async Task Bounded_Void_ExecuteAsync_Task_Carries_Every_Failure()
    {
        var executeTask = Source(10)
            .ForEachAsync(i => (i is 2 or 5 or 8)
                ? Task.FromException(new InvalidOperationException($"item {i} failed"))
                : Task.CompletedTask)
            .ProcessInParallel(maxConcurrency: 2)
            .ExecuteAsync();

        Exception? caught = null;
        try
        {
            await executeTask;
        }
        catch (Exception exception)
        {
            caught = exception;
        }

        await Assert.That(caught is InvalidOperationException).IsTrue();

        var messages = executeTask.Exception!.InnerExceptions.Select(e => e.Message).OrderBy(m => m).ToList();
        await Assert.That(messages.Count).IsEqualTo(3);
        await Assert.That(messages).IsEquivalentTo(new[] { "item 2 failed", "item 5 failed", "item 8 failed" });
    }

    [Test]
    public async Task Batch_Void_ExecuteAsync_Task_Carries_Every_Failure_In_The_Batch()
    {
        var executeTask = Source(2)
            .ForEachAsync(i => Task.FromException(new InvalidOperationException($"item {i} failed")))
            .ProcessInBatches(2)
            .ExecuteAsync();

        try
        {
            await executeTask;
        }
        catch (Exception)
        {
            // Expected
        }

        var messages = executeTask.Exception!.InnerExceptions.Select(e => e.Message).ToList();
        await Assert.That(messages.Count).IsEqualTo(2);
        await Assert.That(messages).IsEquivalentTo(new[] { "item 0 failed", "item 1 failed" });
    }

    [Test]
    public async Task Unbounded_Void_Source_Failure_Stays_Primary_When_InFlight_Tasks_Also_Fail()
    {
        var taskStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var executeTask = ThrowingSource(1)
            .ForEachAsync(async _ =>
            {
                taskStarted.TrySetResult();
                await Task.Yield();
                throw new ApplicationException("in-flight task failed");
            })
            .ProcessInParallel()
            .ExecuteAsync();

        await taskStarted.Task;

        Exception? caught = null;
        try
        {
            await executeTask;
        }
        catch (Exception exception)
        {
            caught = exception;
        }

        // The source failure is primary; the in-flight failure is preserved alongside it
        await Assert.That(caught is InvalidOperationException).IsTrue();
        await Assert.That(caught!.Message).IsEqualTo("source failed");

        var messages = executeTask.Exception!.InnerExceptions.Select(e => e.Message).ToList();
        await Assert.That(messages.Count).IsEqualTo(2);
        await Assert.That(messages.Contains("in-flight task failed")).IsTrue();
    }

    [Test]
    public async Task Unbounded_Result_Stream_Early_Break_Cancels_InFlight_Work()
    {
        var cancelledCount = 0;

        var processor = Source(10)
            .SelectAsync(async (i, cancellationToken) =>
            {
                if (i == 0)
                {
                    return i;
                }

                try
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref cancelledCount);
                    throw;
                }

                return i;
            })
            .ProcessInParallel();

        var stopwatch = Stopwatch.StartNew();

        await foreach (var _ in processor.ExecuteAsync())
        {
            break;
        }

        stopwatch.Stop();

        // Before the fix this blocked forever (the drain waited for every in-flight task and
        // nothing cancelled them). Well under the 30s disposal window proves cancellation ran.
        await Assert.That(stopwatch.Elapsed < TimeSpan.FromSeconds(15)).IsTrue();
        await Assert.That(cancelledCount).IsEqualTo(9);
    }

    [Test]
    public async Task Bounded_Result_Stream_Early_Break_Cancels_InFlight_Work()
    {
        var cancelledCount = 0;

        var processor = Source(10)
            .SelectAsync(async (i, cancellationToken) =>
            {
                if (i == 0)
                {
                    return i;
                }

                try
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref cancelledCount);
                    throw;
                }

                return i;
            })
            .ProcessInParallel(maxConcurrency: 3);

        var stopwatch = Stopwatch.StartNew();

        await foreach (var _ in processor.ExecuteAsync())
        {
            break;
        }

        stopwatch.Stop();

        await Assert.That(stopwatch.Elapsed < TimeSpan.FromSeconds(15)).IsTrue();
        await Assert.That(cancelledCount > 0).IsTrue();
    }

    [Test]
    public async Task Unbounded_Extension_Drains_Started_Tasks_On_MidEnumeration_Failure()
    {
        var completedCount = 0;

        Exception? caught = null;
        try
        {
            _ = await ThrowingSource(3).ProcessInParallel(async i =>
            {
                await Task.Delay(100);
                Interlocked.Increment(ref completedCount);
                return i;
            });
        }
        catch (Exception exception)
        {
            caught = exception;
        }

        await Assert.That(caught is InvalidOperationException).IsTrue();
        await Assert.That(caught!.Message).IsEqualTo("source failed");

        // Before the fix the extension rethrew immediately and abandoned the started tasks
        // fire-and-forget; now they are drained before the exception leaves the method.
        await Assert.That(completedCount).IsEqualTo(3);
    }
}
