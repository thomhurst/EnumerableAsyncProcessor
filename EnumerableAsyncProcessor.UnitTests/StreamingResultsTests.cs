using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.UnitTests;

/// <summary>
/// Guards GetResultsAsyncEnumerable: results must stream in completion order (not input order),
/// every result must arrive exactly once, and enumeration must honour cancellation.
/// The pre-net9 implementation is a separate code path (completion-order buckets instead of
/// Task.WhenEach), so this suite runs on every target framework of the test project.
/// </summary>
public class StreamingResultsTests
{
    [Test]
    public async Task Results_Stream_In_Completion_Order()
    {
        var signals = Enumerable.Range(0, 3)
            .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
            .ToArray();

        await using var processor = Enumerable.Range(0, 3).ToList()
            .SelectAsync(async i =>
            {
                await signals[i].Task;
                return i;
            })
            .ProcessInParallel();

        var enumerator = processor.GetResultsAsyncEnumerable().GetAsyncEnumerator();

        await using (enumerator)
        {
            signals[2].TrySetResult();
            await Assert.That(await enumerator.MoveNextAsync()).IsTrue();
            await Assert.That(enumerator.Current).IsEqualTo(2);

            signals[0].TrySetResult();
            await Assert.That(await enumerator.MoveNextAsync()).IsTrue();
            await Assert.That(enumerator.Current).IsEqualTo(0);

            signals[1].TrySetResult();
            await Assert.That(await enumerator.MoveNextAsync()).IsTrue();
            await Assert.That(enumerator.Current).IsEqualTo(1);

            await Assert.That(await enumerator.MoveNextAsync()).IsFalse();
        }
    }

    [Test]
    public async Task Streaming_Yields_Every_Result_Exactly_Once()
    {
        const int itemCount = 1_000;

        await using var processor = Enumerable.Range(0, itemCount).ToList()
            .SelectAsync(i => Task.FromResult(i))
            .ProcessInParallel();

        var received = new List<int>();

        await foreach (var result in processor.GetResultsAsyncEnumerable())
        {
            received.Add(result);
        }

        await Assert.That(received.Count).IsEqualTo(itemCount);
        await Assert.That(received.Distinct().Count()).IsEqualTo(itemCount);
    }

    [Test]
    public async Task Streaming_Honours_Cancellation_Token()
    {
        var blocker = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var processor = Enumerable.Range(0, 5).ToList()
            .SelectAsync(async i =>
            {
                await blocker.Task;
                return i;
            })
            .ProcessInParallel();

        try
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

            OperationCanceledException? caught = null;
            try
            {
                await foreach (var _ in processor.GetResultsAsyncEnumerable().WithCancellation(cancellationTokenSource.Token))
                {
                }
            }
            catch (OperationCanceledException exception)
            {
                caught = exception;
            }

            await Assert.That(caught).IsNotNull();
        }
        finally
        {
            blocker.TrySetResult();
        }

        await processor.DisposeAsync();
    }
}
