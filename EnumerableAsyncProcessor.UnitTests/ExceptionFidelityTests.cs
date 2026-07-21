using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.UnitTests;

/// <summary>
/// Guards exception propagation fidelity:
/// - a multi-fault task must surface every inner exception (GetBaseException previously kept only one),
/// - one failing item must not stop the remaining items from processing,
/// - completion sources must run continuations asynchronously so user continuations
///   cannot run inline on the completing thread.
/// </summary>
public class ExceptionFidelityTests
{
    [Test]
    public async Task All_Exceptions_From_A_MultiFault_Task_Are_Preserved()
    {
        await using var processor = new[] { 1 }
            .ForEachAsync(_ => Task.WhenAll(
                Task.FromException(new InvalidOperationException("first failure")),
                Task.FromException(new InvalidOperationException("second failure"))))
            .ProcessOneAtATime();

        Exception? caught = null;
        try
        {
            await processor.WaitAsync();
        }
        catch (Exception exception)
        {
            caught = exception;
        }

        await Assert.That(caught).IsNotNull();

        var itemTask = processor.GetEnumerableTasks().Single();
        var messages = itemTask.Exception!.InnerExceptions.Select(e => e.Message).ToList();

        await Assert.That(messages.Count).IsEqualTo(2);
        await Assert.That(messages.Contains("first failure")).IsTrue();
        await Assert.That(messages.Contains("second failure")).IsTrue();
    }

    [Test]
    public async Task Synchronous_Selector_Throw_Faults_Only_That_Item()
    {
        await using var processor = Enumerable.Range(0, 10).ToList()
            .ForEachAsync(i => i == 5
                ? throw new InvalidOperationException("item 5 failed")
                : Task.CompletedTask)
            .ProcessInParallel(2);

        Exception? caught = null;
        try
        {
            await processor.WaitAsync();
        }
        catch (Exception exception)
        {
            caught = exception;
        }

        await Assert.That(caught).IsNotNull();
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.IsCompletedSuccessfully)).IsEqualTo(9);
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.IsFaulted)).IsEqualTo(1);
    }

    [Test]
    public async Task Failed_Items_Do_Not_Prevent_Remaining_Items_From_Processing()
    {
        var processedCount = 0;

        await using var processor = Enumerable.Range(0, 30).ToList()
            .ForEachAsync(async i =>
            {
                Interlocked.Increment(ref processedCount);
                await Task.Yield();

                if (i % 3 == 0)
                {
                    throw new InvalidOperationException($"item {i} failed");
                }
            })
            .ProcessInParallel(maxConcurrency: 3);

        try
        {
            await processor.WaitAsync();
        }
        catch (Exception)
        {
            // Expected - a third of the items fail
        }

        await Assert.That(processedCount).IsEqualTo(30);
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.IsCompletedSuccessfully)).IsEqualTo(20);
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.IsFaulted)).IsEqualTo(10);
    }

    [Test]
    public async Task Streaming_Results_Surface_The_Original_Exception_Unwrapped()
    {
        await using var processor = Enumerable.Range(0, 5).ToList()
            .SelectAsync(i => i == 2
                ? Task.FromException<int>(new InvalidOperationException("item 2 failed"))
                : Task.FromResult(i))
            .ProcessInParallel(2);

        Exception? caught = null;
        try
        {
            await foreach (var _ in processor.GetResultsAsyncEnumerable())
            {
            }
        }
        catch (Exception exception)
        {
            caught = exception;
        }

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.GetType()).IsEqualTo(typeof(InvalidOperationException));
        await Assert.That(caught.Message).IsEqualTo("item 2 failed");
    }

    [Test]
    public async Task Streaming_Results_Surface_Cancellation_As_OperationCanceledException_Not_AggregateException()
    {
        using var itemCancellation = new CancellationTokenSource();
        itemCancellation.Cancel();

        await using var processor = new[] { 1 }
            .SelectAsync(_ => Task.FromCanceled<int>(itemCancellation.Token))
            .ProcessInParallel(2);

        Exception? caught = null;
        try
        {
            await foreach (var _ in processor.GetResultsAsyncEnumerable())
            {
            }
        }
        catch (Exception exception)
        {
            caught = exception;
        }

        await Assert.That(caught is OperationCanceledException).IsTrue();
    }

    [Test]
    public async Task Item_Task_Continuations_Are_Not_Forced_Inline()
    {
        await using var processor = new[] { 1 }
            .ForEachAsync(_ => Task.CompletedTask)
            .ProcessOneAtATime();

        await processor.WaitAsync();

        var itemTask = processor.GetEnumerableTasks().Single();
        await Assert.That(itemTask.CreationOptions.HasFlag(TaskCreationOptions.RunContinuationsAsynchronously)).IsTrue();
    }
}
