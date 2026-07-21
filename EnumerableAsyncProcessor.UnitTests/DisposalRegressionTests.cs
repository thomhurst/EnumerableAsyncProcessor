using System;
using System.Diagnostics;
using System.Linq;
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
}
