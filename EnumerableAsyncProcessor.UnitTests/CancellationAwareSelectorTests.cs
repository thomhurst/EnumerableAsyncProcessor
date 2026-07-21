using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Builders;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.UnitTests;

public class CancellationAwareSelectorTests
{
    [Test]
    public async Task CancelAll_Interrupts_InFlight_ForEachAsync_Selector(CancellationToken cancellationToken)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var interrupted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var processor = new[] { 1 }
            .ForEachAsync((_, processorToken) => WaitUntilCanceledAsync(processorToken, started, interrupted))
            .ProcessInParallel();

        await started.Task.WaitAsync(cancellationToken);
        processor.CancelAll();

        await interrupted.Task.WaitAsync(cancellationToken);
        await Assert.ThrowsAsync<TaskCanceledException>(() => processor.WaitAsync());
    }

    [Test]
    public async Task CancelAll_Interrupts_InFlight_SelectAsync_Selector(CancellationToken cancellationToken)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var interrupted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var processor = new[] { 1 }
            .SelectAsync((item, processorToken) => WaitUntilCanceledAsync(item, processorToken, started, interrupted))
            .ProcessInParallel();

        await started.Task.WaitAsync(cancellationToken);
        processor.CancelAll();

        await interrupted.Task.WaitAsync(cancellationToken);
        await Assert.ThrowsAsync<TaskCanceledException>(() => processor.GetResultsAsync());
    }

    [Test]
    public async Task CancelAll_Interrupts_ExecutionCount_Selector(CancellationToken cancellationToken)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var interrupted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var processor = AsyncProcessorBuilder.WithExecutionCount(1)
            .ForEachAsync(processorToken => WaitUntilCanceledAsync(processorToken, started, interrupted))
            .ProcessInParallel();

        await started.Task.WaitAsync(cancellationToken);
        processor.CancelAll();

        await interrupted.Task.WaitAsync(cancellationToken);
        await Assert.ThrowsAsync<TaskCanceledException>(() => processor.WaitAsync());
    }

    [Test]
    public async Task DisposeAsync_Interrupts_InFlight_Selector(CancellationToken cancellationToken)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var interrupted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var processor = new[] { 1 }
            .ForEachAsync((_, processorToken) => WaitUntilCanceledAsync(processorToken, started, interrupted))
            .ProcessInParallel();

        await started.Task.WaitAsync(cancellationToken);
        var disposeTask = processor.DisposeAsync().AsTask();

        await interrupted.Task.WaitAsync(cancellationToken);
        await disposeTask.WaitAsync(cancellationToken);
    }

    [Test]
    public async Task ExternalCancellation_Interrupts_AsyncEnumerable_Selector(CancellationToken cancellationToken)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var interrupted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var processor = GetItemsAsync()
            .ForEachAsync(
                (_, processorToken) => WaitUntilCanceledAsync(processorToken, started, interrupted),
                cancellationTokenSource.Token)
            .ProcessInParallel();

        var executeTask = processor.ExecuteAsync();
        await started.Task.WaitAsync(cancellationToken);
        await cancellationTokenSource.CancelAsync();

        await interrupted.Task.WaitAsync(cancellationToken);
        await Assert.ThrowsAsync<OperationCanceledException>(() => executeTask);
    }

    private static async Task WaitUntilCanceledAsync(
        CancellationToken cancellationToken,
        TaskCompletionSource started,
        TaskCompletionSource interrupted)
    {
        started.TrySetResult();
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            interrupted.TrySetResult();
            throw;
        }
    }

    private static async Task<T> WaitUntilCanceledAsync<T>(
        T result,
        CancellationToken cancellationToken,
        TaskCompletionSource started,
        TaskCompletionSource interrupted)
    {
        await WaitUntilCanceledAsync(cancellationToken, started, interrupted);
        return result;
    }

    private static async IAsyncEnumerable<int> GetItemsAsync()
    {
        yield return 1;
        await Task.CompletedTask;
    }
}
