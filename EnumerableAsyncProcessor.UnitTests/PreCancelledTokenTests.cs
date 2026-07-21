using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;
using TUnit.Assertions;
using TUnit.Core;

namespace EnumerableAsyncProcessor.UnitTests;

/// <summary>
/// Guards the pre-cancelled token contract (issue #367):
/// building a processor with an already-cancelled token must produce a processor whose
/// per-item tasks are cancelled - matching TPL convention - instead of throwing
/// ArgumentException from an internal constructor parameter the caller never passed.
/// </summary>
public class PreCancelledTokenTests
{
    [Test]
    public async Task Void_Processor_Built_With_PreCancelled_Token_Yields_Cancelled_Tasks()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var invoked = 0;

        await using var processor = Enumerable.Range(0, 5).ToList()
            .ForEachAsync(_ =>
            {
                Interlocked.Increment(ref invoked);
                return Task.CompletedTask;
            }, cancellationTokenSource.Token)
            .ProcessInParallel(maxConcurrency: 2);

        Exception? caught = null;
        try
        {
            await processor.WaitAsync();
        }
        catch (Exception exception)
        {
            caught = exception;
        }

        await Assert.That(caught is OperationCanceledException).IsTrue();
        await Assert.That(processor.GetEnumerableTasks().All(t => t.IsCanceled)).IsTrue();
        await Assert.That(invoked).IsEqualTo(0);
    }

    [Test]
    public async Task Result_Processor_Built_With_PreCancelled_Token_Yields_Cancelled_Tasks()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await using var processor = Enumerable.Range(0, 5).ToList()
            .SelectAsync(i => Task.FromResult(i), cancellationTokenSource.Token)
            .ProcessInParallel(maxConcurrency: 2);

        Exception? caught = null;
        try
        {
            _ = await processor.GetResultsAsync();
        }
        catch (Exception exception)
        {
            caught = exception;
        }

        await Assert.That(caught is OperationCanceledException).IsTrue();
        await Assert.That(processor.GetEnumerableTasks().All(t => t.IsCanceled)).IsTrue();
    }

    [Test]
    public async Task OneAtATime_And_Batch_Built_With_PreCancelled_Token_Yield_Cancelled_Tasks()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await using var oneAtATime = new[] { 1, 2, 3 }
            .ForEachAsync(_ => Task.CompletedTask, cancellationTokenSource.Token)
            .ProcessOneAtATime();

        await using var batched = new[] { 1, 2, 3 }
            .ForEachAsync(_ => Task.CompletedTask, cancellationTokenSource.Token)
            .ProcessInBatches(2);

        await Assert.That(oneAtATime.GetEnumerableTasks().All(t => t.IsCanceled)).IsTrue();
        await Assert.That(batched.GetEnumerableTasks().All(t => t.IsCanceled)).IsTrue();
    }
}
