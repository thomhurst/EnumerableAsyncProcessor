#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;
using System.Runtime.CompilerServices;
using TUnit.Assertions;
using TUnit.Core;

namespace EnumerableAsyncProcessor.UnitTests;

public class AsyncEnumerableProcessorTests
{
    private static async IAsyncEnumerable<int> GenerateAsyncEnumerable(int count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 1; i <= count; i++)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return i;
        }
    }

    [Test]
    public async Task ForEachAsync_ProcessOneAtATime_ProcessesAllItems()
    {
        var processedItems = new List<int>();
        var asyncEnumerable = GenerateAsyncEnumerable(10);

        await asyncEnumerable
            .ForEachAsync(async item =>
            {
                await Task.Delay(10);
                lock (processedItems)
                {
                    processedItems.Add(item);
                }
            })
            .ProcessOneAtATime()
            .ExecuteAsync();

        await Assert.That(processedItems.Count).IsEqualTo(10);
        await Assert.That(processedItems).IsEquivalentTo(Enumerable.Range(1, 10));
    }

    [Test]
    public async Task ForEachAsync_ProcessInParallel_ProcessesAllItems()
    {
        var processedItems = new List<int>();
        var asyncEnumerable = GenerateAsyncEnumerable(20);

        await asyncEnumerable
            .ForEachAsync(async item =>
            {
                await Task.Delay(10);
                lock (processedItems)
                {
                    processedItems.Add(item);
                }
            })
            .ProcessInParallel(5)
            .ExecuteAsync();

        await Assert.That(processedItems.Count).IsEqualTo(20);
        await Assert.That(processedItems.OrderBy(x => x)).IsEquivalentTo(Enumerable.Range(1, 20));
    }

    [Test]
    public async Task SelectAsync_ProcessOneAtATime_ReturnsTransformedItems()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(5);

        var results = await asyncEnumerable
            .SelectAsync(async item =>
            {
                await Task.Delay(10);
                return item * 2;
            })
            .ProcessOneAtATime()
            .ExecuteAsync()
            .ToListAsync();

        await Assert.That(results.Count).IsEqualTo(5);
        await Assert.That(results).IsEquivalentTo(new[] { 2, 4, 6, 8, 10 });
    }

    [Test]
    public async Task SelectAsync_ProcessInParallel_ReturnsAllTransformedItems()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(10);

        var results = await asyncEnumerable
            .SelectAsync(async item =>
            {
                await Task.Delay(10);
                return item * 2;
            })
            .ProcessInParallel(3)
            .ExecuteAsync()
            .ToListAsync();

        await Assert.That(results.Count).IsEqualTo(10);
        await Assert.That(results.OrderBy(x => x)).IsEquivalentTo(Enumerable.Range(1, 10).Select(x => x * 2));
    }

    [Test]
    public async Task ForEachAsync_ProcessInParallelForIO_HandlesHighConcurrency()
    {
        var processedCount = 0;
        var asyncEnumerable = GenerateAsyncEnumerable(100);

        await asyncEnumerable
            .ForEachAsync(async item =>
            {
                await Task.Delay(5);
                Interlocked.Increment(ref processedCount);
            })
            .ProcessInParallelForIO(50)
            .ExecuteAsync();

        await Assert.That(processedCount).IsEqualTo(100);
    }

    [Test]
    public async Task SelectAsync_ProcessInParallelForIO_HandlesHighConcurrency()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(50);

        var results = await asyncEnumerable
            .SelectAsync(async item =>
            {
                await Task.Delay(5);
                return item * 3;
            })
            .ProcessInParallelForIO(25)
            .ExecuteAsync()
            .ToListAsync();

        await Assert.That(results.Count).IsEqualTo(50);
        await Assert.That(results.OrderBy(x => x)).IsEquivalentTo(Enumerable.Range(1, 50).Select(x => x * 3));
    }

    [Test]
    public async Task ForEachAsync_ProcessWithChannel_ProcessesAllItems()
    {
        var processedItems = new List<int>();
        var asyncEnumerable = GenerateAsyncEnumerable(30);

        await asyncEnumerable
            .ForEachAsync(async item =>
            {
                await Task.Delay(5);
                lock (processedItems)
                {
                    processedItems.Add(item);
                }
            })
            .ProcessWithChannel(new AsyncEnumerableChannelOptions 
            { 
                BufferSize = 10,
                MaxConcurrency = 5
            })
            .ExecuteAsync();

        await Assert.That(processedItems.Count).IsEqualTo(30);
        await Assert.That(processedItems.OrderBy(x => x)).IsEquivalentTo(Enumerable.Range(1, 30));
    }

    [Test]
    public async Task SelectAsync_ProcessWithChannel_WithOrderPreservation_MaintainsOrder()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(20);
        var random = new Random(42);

        var results = await asyncEnumerable
            .SelectAsync(async item =>
            {
                // Random delay to test order preservation
                await Task.Delay(random.Next(1, 20));
                return item * 2;
            })
            .ProcessWithChannel(new AsyncEnumerableChannelOptions 
            { 
                BufferSize = 5,
                MaxConcurrency = 4,
                PreserveOrder = true
            })
            .ExecuteAsync()
            .ToListAsync();

        await Assert.That(results.Count).IsEqualTo(20);
        await Assert.That(results).IsEquivalentTo(Enumerable.Range(1, 20).Select(x => x * 2));
    }

    [Test]
    public async Task SelectAsync_ProcessWithChannel_WithoutOrderPreservation_ReturnsAllItems()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(20);

        var results = await asyncEnumerable
            .SelectAsync(async item =>
            {
                await Task.Delay(5);
                return item * 2;
            })
            .ProcessWithChannel(new AsyncEnumerableChannelOptions 
            { 
                PreserveOrder = false,
                MaxConcurrency = 4
            })
            .ExecuteAsync()
            .ToListAsync();

        await Assert.That(results.Count).IsEqualTo(20);
        await Assert.That(results.OrderBy(x => x)).IsEquivalentTo(Enumerable.Range(1, 20).Select(x => x * 2));
    }

    [Test]
    public async Task ForEachAsync_WithCancellation_StopsProcessing()
    {
        var cts = new CancellationTokenSource();
        var processedCount = 0;
        var asyncEnumerable = GenerateAsyncEnumerable(100, cts.Token);

        var task = asyncEnumerable
            .ForEachAsync(async item =>
            {
                if (item == 10)
                {
                    cts.Cancel();
                }
                await Task.Delay(10);
                Interlocked.Increment(ref processedCount);
            }, cts.Token)
            .ProcessInParallel(5)
            .ExecuteAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
        await Assert.That(processedCount).IsLessThan(100);
    }

    [Test]
    public async Task SelectAsync_WithEmptyAsyncEnumerable_ReturnsEmptyResults()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(0);

        var results = await asyncEnumerable
            .SelectAsync(async item =>
            {
                await Task.Delay(10);
                return item * 2;
            })
            .ProcessInParallel(5)
            .ExecuteAsync()
            .ToListAsync();

        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task ForEachAsync_WithException_PropagatesException()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(10);

        var task = asyncEnumerable
            .ForEachAsync(async item =>
            {
                await Task.Delay(10);
                if (item == 5)
                {
                    throw new InvalidOperationException("Test exception");
                }
            })
            .ProcessInParallel(3)
            .ExecuteAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
        await Assert.That(exception!.Message).IsEqualTo("Test exception");
    }
}

internal static class AsyncEnumerableExtensionsForTests
{
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source)
        {
            list.Add(item);
        }
        return list;
    }
}
#endif