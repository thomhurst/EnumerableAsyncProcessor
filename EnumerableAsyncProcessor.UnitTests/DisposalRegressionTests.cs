using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.UnitTests;

/// <summary>
/// Guards the disposal and cancellation semantics:
/// - disposal must actually cancel pending work (a guard-ordering bug previously made it a no-op),
/// - synchronous Dispose must not block (it previously ran sync-over-async with a 30s wait),
/// - CancelAll on a result processor must not block the calling thread (it previously invoked
///   blocking Dispose via the cancellation callback).
/// </summary>
public class DisposalRegressionTests
{
    [Test]
    public async Task DisposeAsync_Cancels_Unstarted_Tasks_And_Completes_Promptly()
    {
        var blocker = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstItemStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var processor = Enumerable.Range(0, 10).ToList()
            .ForEachAsync(async _ =>
            {
                firstItemStarted.TrySetResult();
                await blocker.Task;
            })
            .ProcessInParallel(1);

        await firstItemStarted.Task;

        var stopwatch = Stopwatch.StartNew();
        var disposeTask = processor.DisposeAsync();

        // Cancellation happens synchronously inside DisposeAsync, before it waits for in-flight work.
        // Previously nothing was cancelled and disposal just waited for tasks to finish naturally.
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.IsCanceled)).IsEqualTo(10);

        blocker.TrySetResult();
        await disposeTask;
        stopwatch.Stop();

        await Assert.That(stopwatch.Elapsed).IsLessThan(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task Synchronous_Dispose_Returns_Without_Blocking_On_InFlight_Work()
    {
        var blocker = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstItemStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var processor = Enumerable.Range(0, 10).ToList()
            .ForEachAsync(async _ =>
            {
                firstItemStarted.TrySetResult();
                await blocker.Task;
            })
            .ProcessInParallel(1);

        await firstItemStarted.Task;

        try
        {
            var stopwatch = Stopwatch.StartNew();
            processor.Dispose();
            stopwatch.Stop();

            await Assert.That(stopwatch.Elapsed).IsLessThan(TimeSpan.FromSeconds(2));
            await Assert.That(processor.GetEnumerableTasks().Count(x => x.IsCanceled)).IsEqualTo(10);
        }
        finally
        {
            blocker.TrySetResult();
        }
    }

    [Test]
    public async Task CancelAll_On_Result_Processor_Does_Not_Block_And_Cancels_Pending_Tasks()
    {
        var blocker = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstItemStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var processor = Enumerable.Range(0, 10).ToList()
            .SelectAsync(async i =>
            {
                firstItemStarted.TrySetResult();
                await blocker.Task;
                return i;
            })
            .ProcessInParallel(1);

        await firstItemStarted.Task;

        try
        {
            var stopwatch = Stopwatch.StartNew();
            processor.CancelAll();
            stopwatch.Stop();

            await Assert.That(stopwatch.Elapsed).IsLessThan(TimeSpan.FromSeconds(2));
            await Assert.That(processor.GetEnumerableTasks().Count(x => x.IsCanceled)).IsEqualTo(10);
            await Assert.ThrowsAsync<TaskCanceledException>(() => processor.GetResultsAsync());
        }
        finally
        {
            blocker.TrySetResult();
        }

        await processor.DisposeAsync();
    }

    [Test]
    public async Task Disposal_Is_Idempotent_And_Safe_In_Any_Order()
    {
        var processor = Enumerable.Range(0, 5).ToList()
            .ForEachAsync(_ => Task.CompletedTask)
            .ProcessInParallel();

        await processor.WaitAsync();

        await processor.DisposeAsync();
        await processor.DisposeAsync();
        processor.Dispose();
        processor.CancelAll();

        await Assert.That(processor.GetEnumerableTasks().Count(x => x.IsCompletedSuccessfully)).IsEqualTo(5);
    }

    [Test]
    public async Task AsyncEnumerable_Processor_Disposal_Is_Idempotent_After_Execution()
    {
        var processedCount = 0;

        var processor = GenerateAsyncEnumerable(5)
            .ForEachAsync(_ =>
            {
                Interlocked.Increment(ref processedCount);
                return Task.CompletedTask;
            })
            .ProcessInParallel(maxConcurrency: 2);

        await processor.ExecuteAsync();

        // ExecuteAsync disposes internal resources on completion; explicit disposal stays safe.
        await processor.DisposeAsync();
        processor.Dispose();

        await Assert.That(processedCount).IsEqualTo(5);
    }

    [Test]
    public async Task AsyncEnumerable_Result_Processor_Supports_Await_Using_Without_Execution()
    {
        await using (GenerateAsyncEnumerable(3).SelectAsync(i => Task.FromResult(i)).ProcessInParallel(2))
        {
            // Never executed - disposal alone must not throw.
        }
    }

    [Test, Timeout(30_000)]
    public async Task AsyncEnumerable_Processor_Dispose_During_Execution_Cancels_Processing(CancellationToken cancellationToken)
    {
        var firstItemStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var processor = InfiniteAsyncEnumerable()
            .ForEachAsync(async _ =>
            {
                firstItemStarted.TrySetResult();
                await Task.Yield();
            })
            .ProcessInParallel(maxConcurrency: 1);

        var executeTask = processor.ExecuteAsync();
        await firstItemStarted.Task;

        processor.Dispose();

        Exception? caught = null;
        try
        {
            await executeTask.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        }
        catch (Exception exception)
        {
            caught = exception;
        }

        // A TimeoutException here means disposal did not cancel the in-flight run.
        await Assert.That(caught is OperationCanceledException).IsTrue();
    }

    private static async IAsyncEnumerable<int> InfiniteAsyncEnumerable(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var i = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return i++;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<int> GenerateAsyncEnumerable(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }
}
