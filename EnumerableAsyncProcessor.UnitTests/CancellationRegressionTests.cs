using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.UnitTests;

/// <summary>
/// Guards against cancellation hanging the bounded async-enumerable result pipeline:
/// a worker that observed cancellation between an item being written to the channel
/// and that item being claimed used to abandon the item's completion source, leaving
/// the consumer awaiting a task that never completes.
/// </summary>
public class CancellationRegressionTests
{
    [Test, Timeout(120_000)]
    public async Task Cancelling_Bounded_Result_Streaming_Never_Hangs(CancellationToken cancellationToken)
    {
        for (var iteration = 0; iteration < 100; iteration++)
        {
            using var cts = new CancellationTokenSource();
            var cancelAfter = 1 + iteration % 10;
            var consumed = 0;

            var stream = InfiniteSource()
                .SelectAsync(async item =>
                {
                    await Task.Yield();
                    return item;
                }, cts.Token)
                .ProcessInParallel(maxConcurrency: 2);

            async Task ConsumeAsync()
            {
                await foreach (var _ in stream.ExecuteAsync())
                {
                    if (Interlocked.Increment(ref consumed) == cancelAfter)
                    {
                        cts.Cancel();
                    }
                }
            }

            Exception? caught = null;
            try
            {
                await ConsumeAsync().WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
            catch (Exception exception)
            {
                caught = exception;
            }

            // A TimeoutException here means the pipeline hung instead of observing cancellation.
            await Assert.That(caught is OperationCanceledException).IsTrue();
        }
    }

    // Deliberately ignores cancellation so the pipeline's own guards are what stop it.
    private static async IAsyncEnumerable<int> InfiniteSource()
    {
        var i = 0;
        while (true)
        {
            yield return i++;
            await Task.Yield();
        }
    }
}
