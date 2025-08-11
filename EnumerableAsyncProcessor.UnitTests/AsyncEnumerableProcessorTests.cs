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
    public async Task ForEachAsync_ProcessInParallel_WithHighConcurrency_HandlesCorrectly()
    {
        var processedCount = 0;
        var asyncEnumerable = GenerateAsyncEnumerable(100);

        await asyncEnumerable
            .ForEachAsync(async item =>
            {
                await Task.Delay(5);
                Interlocked.Increment(ref processedCount);
            })
            .ProcessInParallel(50)
            .ExecuteAsync();

        await Assert.That(processedCount).IsEqualTo(100);
    }

    [Test]
    public async Task SelectAsync_ProcessInParallel_WithHighConcurrency_HandlesCorrectly()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(50);

        var results = await asyncEnumerable
            .SelectAsync(async item =>
            {
                await Task.Delay(5);
                return item * 3;
            })
            .ProcessInParallel(25)
            .ExecuteAsync()
            .ToListAsync();

        await Assert.That(results.Count).IsEqualTo(50);
        await Assert.That(results.OrderBy(x => x)).IsEquivalentTo(Enumerable.Range(1, 50).Select(x => x * 3));
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
                // Check cancellation before processing
                if (cts.Token.IsCancellationRequested)
                    return;
                    
                if (item == 10)
                {
                    cts.Cancel();
                }
                
                await Task.Delay(10);
                
                // Check cancellation after delay
                if (cts.Token.IsCancellationRequested)
                    return;
                    
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
    
    [Test]
    public async Task ForEachAsync_ProcessInParallel_UnboundedConcurrency_ProcessesAllItems()
    {
        var processedItems = new List<int>();
        var asyncEnumerable = GenerateAsyncEnumerable(50);

        await asyncEnumerable
            .ForEachAsync(async item =>
            {
                await Task.Delay(5);
                lock (processedItems)
                {
                    processedItems.Add(item);
                }
            })
            .ProcessInParallel() // Unbounded concurrency
            .ExecuteAsync();

        await Assert.That(processedItems.Count).IsEqualTo(50);
        await Assert.That(processedItems.OrderBy(x => x)).IsEquivalentTo(Enumerable.Range(1, 50));
    }
    
    [Test]
    public async Task SelectAsync_ProcessInParallel_UnboundedConcurrency_ReturnsAllResults()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(30);

        var results = await asyncEnumerable
            .SelectAsync(async item =>
            {
                await Task.Delay(5);
                return item * 2;
            })
            .ProcessInParallel() // Unbounded concurrency
            .ExecuteAsync()
            .ToListAsync();

        await Assert.That(results.Count).IsEqualTo(30);
        await Assert.That(results.OrderBy(x => x)).IsEquivalentTo(Enumerable.Range(1, 30).Select(x => x * 2));
    }
    
    [Test]
    public async Task ForEachAsync_ProcessInParallel_WithThreadPoolScheduling_ProcessesAllItems()
    {
        var processedItems = new List<int>();
        var asyncEnumerable = GenerateAsyncEnumerable(20);

        await asyncEnumerable
            .ForEachAsync(async item =>
            {
                await Task.Delay(5);
                lock (processedItems)
                {
                    processedItems.Add(item);
                }
            })
            .ProcessInParallel(scheduleOnThreadPool: true)
            .ExecuteAsync();

        await Assert.That(processedItems.Count).IsEqualTo(20);
        await Assert.That(processedItems.OrderBy(x => x)).IsEquivalentTo(Enumerable.Range(1, 20));
    }
    
    [Test]
    public async Task ForEachAsync_ProcessInBatches_ProcessesAllItemsInBatches()
    {
        var processedBatches = new List<int>();
        var asyncEnumerable = GenerateAsyncEnumerable(25);

        await asyncEnumerable
            .ForEachAsync(async item =>
            {
                await Task.Delay(5);
                lock (processedBatches)
                {
                    processedBatches.Add(item);
                }
            })
            .ProcessInBatches(5)
            .ExecuteAsync();

        await Assert.That(processedBatches.Count).IsEqualTo(25);
        await Assert.That(processedBatches.OrderBy(x => x)).IsEquivalentTo(Enumerable.Range(1, 25));
    }
    
    [Test]
    public async Task SelectAsync_ProcessInBatches_ReturnsAllResultsInBatches()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(23);

        var results = await asyncEnumerable
            .SelectAsync(async item =>
            {
                await Task.Delay(5);
                return item * 3;
            })
            .ProcessInBatches(5)
            .ExecuteAsync()
            .ToListAsync();

        await Assert.That(results.Count).IsEqualTo(23);
        // Batches maintain order within batch, so results should be in order
        await Assert.That(results).IsEquivalentTo(Enumerable.Range(1, 23).Select(x => x * 3));
    }
    
    [Test]
    public async Task ProcessInParallel_NullableConcurrency_WorksCorrectly()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(15);
        var processedCount = 0;

        // Test with null concurrency (unbounded)
        await asyncEnumerable
            .ForEachAsync(async item =>
            {
                await Task.Delay(5);
                Interlocked.Increment(ref processedCount);
            })
            .ProcessInParallel((int?)null)
            .ExecuteAsync();

        await Assert.That(processedCount).IsEqualTo(15);
        
        // Reset and test with specified concurrency
        processedCount = 0;
        asyncEnumerable = GenerateAsyncEnumerable(15);
        
        await asyncEnumerable
            .ForEachAsync(async item =>
            {
                await Task.Delay(5);
                Interlocked.Increment(ref processedCount);
            })
            .ProcessInParallel((int?)5)
            .ExecuteAsync();

        await Assert.That(processedCount).IsEqualTo(15);
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