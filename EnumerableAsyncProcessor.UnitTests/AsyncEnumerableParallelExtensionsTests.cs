#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;
using TUnit.Assertions;
using TUnit.Core;

namespace EnumerableAsyncProcessor.UnitTests;

public class AsyncEnumerableParallelExtensionsTests
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

    private static async IAsyncEnumerable<int> GenerateDelayedAsyncEnumerable(int count, int delayMs, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 1; i <= count; i++)
        {
            await Task.Delay(delayMs, cancellationToken);
            yield return i;
        }
    }

    [Test]
    public async Task ProcessInParallel_WithoutTransformation_ReturnsAllItems()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(10);
        
        var results = await asyncEnumerable.ProcessInParallel();
        
        await Assert.That(results.Count()).IsEqualTo(10);
        await Assert.That(results.OrderBy(x => x)).IsEquivalentTo(Enumerable.Range(1, 10));
    }

    [Test]
    public async Task ProcessInParallel_WithMaxConcurrency_ReturnsAllItems()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(20);
        
        var results = await asyncEnumerable.ProcessInParallel(5);
        
        await Assert.That(results.Count()).IsEqualTo(20);
        await Assert.That(results.OrderBy(x => x)).IsEquivalentTo(Enumerable.Range(1, 20));
    }

    [Test]
    public async Task ProcessInParallel_WithTransformation_ReturnsTransformedItems()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(5);
        
        var results = await asyncEnumerable.ProcessInParallel(
            async item =>
            {
                await Task.Delay(10);
                return item * 2;
            });
        
        await Assert.That(results.Count()).IsEqualTo(5);
        await Assert.That(results.OrderBy(x => x)).IsEquivalentTo(new[] { 2, 4, 6, 8, 10 });
    }

    [Test]
    public async Task ProcessInParallel_WithTransformationAndMaxConcurrency_ReturnsTransformedItems()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(10);
        
        var results = await asyncEnumerable.ProcessInParallel(
            async item =>
            {
                await Task.Delay(10);
                return item.ToString();
            },
            maxConcurrency: 3);
        
        await Assert.That(results.Count()).IsEqualTo(10);
        await Assert.That(results.OrderBy(x => int.Parse(x))).IsEquivalentTo(
            Enumerable.Range(1, 10).Select(i => i.ToString()));
    }

    [Test]
    public async Task ProcessInParallel_WithCancellation_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        var asyncEnumerable = GenerateDelayedAsyncEnumerable(100, 50);
        
        var task = asyncEnumerable.ProcessInParallel(cancellationToken: cts.Token);
        
        // Cancel after a short delay
        cts.CancelAfter(100);
        
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
    }

    [Test]
    public async Task ProcessInParallel_ActuallyRunsInParallel()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(5);
        var stopwatch = Stopwatch.StartNew();
        
        var results = await asyncEnumerable.ProcessInParallel(
            async item =>
            {
                await Task.Delay(100); // Each item takes 100ms
                return item;
            });
        
        stopwatch.Stop();
        
        // If running sequentially, it would take ~500ms
        // In parallel, it should take ~100ms (plus some overhead)
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(250);
        await Assert.That(results.Count()).IsEqualTo(5);
    }

    [Test]
    public async Task ProcessInParallel_WithMaxConcurrency_LimitsConcurrency()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(10);
        var maxConcurrentTasks = 0;
        var currentConcurrentTasks = 0;
        var lockObj = new object();
        
        var results = await asyncEnumerable.ProcessInParallel(
            async item =>
            {
                lock (lockObj)
                {
                    currentConcurrentTasks++;
                    maxConcurrentTasks = Math.Max(maxConcurrentTasks, currentConcurrentTasks);
                }
                
                await Task.Delay(50); // Simulate work
                
                lock (lockObj)
                {
                    currentConcurrentTasks--;
                }
                
                return item;
            },
            maxConcurrency: 3);
        
        await Assert.That(maxConcurrentTasks).IsLessThanOrEqualTo(3);
        await Assert.That(results.Count()).IsEqualTo(10);
    }

    [Test]
    public async Task ProcessInParallel_EmptyEnumerable_ReturnsEmptyResult()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(0);
        
        var results = await asyncEnumerable.ProcessInParallel();
        
        await Assert.That(results.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task ProcessInParallel_WithScheduleOnThreadPool_ProcessesAllItems()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(10);
        
        var results = await asyncEnumerable.ProcessInParallel(
            maxConcurrency: null,
            scheduleOnThreadPool: true);
        
        await Assert.That(results.Count()).IsEqualTo(10);
        await Assert.That(results.OrderBy(x => x)).IsEquivalentTo(Enumerable.Range(1, 10));
    }

    [Test]
    public async Task ProcessInParallel_WithTransformationScheduleOnThreadPool_ProcessesAllItems()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(10);
        
        var results = await asyncEnumerable.ProcessInParallel(
            async item =>
            {
                await Task.Delay(10);
                return item * 3;
            },
            maxConcurrency: null,
            scheduleOnThreadPool: true);
        
        await Assert.That(results.Count()).IsEqualTo(10);
        await Assert.That(results.OrderBy(x => x)).IsEquivalentTo(
            Enumerable.Range(1, 10).Select(i => i * 3));
    }

    [Test]
    public async Task ProcessInParallel_PreservesExceptionFromTransformation()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(5);
        
        var task = asyncEnumerable.ProcessInParallel(
            async item =>
            {
                await Task.Delay(10);
                if (item == 3)
                {
                    throw new InvalidOperationException("Test exception");
                }
                return item;
            });
        
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
        await Assert.That(exception.Message).IsEqualTo("Test exception");
    }
}
#endif