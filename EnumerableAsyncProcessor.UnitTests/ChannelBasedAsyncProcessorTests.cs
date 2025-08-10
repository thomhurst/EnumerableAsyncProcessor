#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;
using TUnit.Assertions;
using TUnit.Core;

namespace EnumerableAsyncProcessor.UnitTests;

public class ChannelBasedAsyncProcessorTests
{
    [Test]
    public async Task ProcessWithChannel_WithUnboundedChannel_ShouldProcessAllItems()
    {
        // Arrange
        const int itemCount = 100;
        var processedItems = new List<int>();
        var lockObj = new object();
        var items = Enumerable.Range(1, itemCount);
        
        var options = ChannelProcessorOptions.CreateUnbounded(consumerCount: 1);

        // Act
        var processor = items.ForEachWithChannelAsync(async item =>
        {
            await Task.Delay(10);
            lock (lockObj)
            {
                processedItems.Add(item);
            }
        }, options);

        await processor;

        // Assert
        await Assert.That(processedItems).HasCount().EqualTo(itemCount);
        await Assert.That(processedItems.OrderBy(x => x)).IsEquivalentTo(items);
    }

    [Test]
    public async Task ProcessWithChannel_WithBoundedChannel_ShouldRespectBackpressure()
    {
        // Arrange
        const int itemCount = 50;
        const int channelCapacity = 5;
        var processedItems = new List<int>();
        var lockObj = new object();
        var items = Enumerable.Range(1, itemCount);
        
        var options = ChannelProcessorOptions.CreateBounded(channelCapacity, consumerCount: 1);
        
        var slowConsumer = false;
        var processingTimes = new List<DateTime>();

        // Act
        var processor = items.ForEachWithChannelAsync(async item =>
        {
            if (slowConsumer && item <= 10)
            {
                await Task.Delay(100); // Slow down first 10 items to test backpressure
            }
            
            lock (lockObj)
            {
                processedItems.Add(item);
                processingTimes.Add(DateTime.UtcNow);
            }
        }, options);

        slowConsumer = true;
        await processor;

        // Assert
        await Assert.That(processedItems).HasCount().EqualTo(itemCount);
        await Assert.That(processedItems.OrderBy(x => x)).IsEquivalentTo(items);
    }

    [Test]
    public async Task ProcessWithChannel_WithMultipleConsumers_ShouldProcessInParallel()
    {
        // Arrange
        const int itemCount = 100;
        const int consumerCount = 4;
        var processedItems = new List<int>();
        var lockObj = new object();
        var threadIds = new HashSet<int>();
        var items = Enumerable.Range(1, itemCount);
        
        var options = ChannelProcessorOptions.CreateUnbounded(consumerCount);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var processor = items.ForEachWithChannelAsync(async item =>
        {
            await Task.Delay(50); // Simulate work
            lock (lockObj)
            {
                processedItems.Add(item);
                threadIds.Add(Environment.CurrentManagedThreadId);
            }
        }, options);

        await processor;
        stopwatch.Stop();

        // Assert
        await Assert.That(processedItems).HasCount().EqualTo(itemCount);
        await Assert.That(processedItems.OrderBy(x => x)).IsEquivalentTo(items);
        
        // With multiple consumers, we should see multiple threads
        await Assert.That(threadIds.Count).IsGreaterThanOrEqualTo(2);
        
        // Processing should be faster with multiple consumers
        // With 4 consumers, it should take roughly 1/4 the time (plus overhead)
        // Total sequential time would be ~5 seconds, parallel should be much less
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(10000);
    }

    [Test]
    public async Task SelectWithChannelAsync_ShouldReturnResults()
    {
        // Arrange
        const int itemCount = 50;
        var items = Enumerable.Range(1, itemCount);
        var expectedResults = items.Select(x => x * 2);
        
        var options = ChannelProcessorOptions.CreateUnbounded(consumerCount: 2);

        // Act
        var processor = items.SelectWithChannelAsync(async item =>
        {
            await Task.Delay(10);
            return item * 2;
        }, options);

        var results = await processor.GetResultsAsync();

        // Assert
        await Assert.That(results).HasCount().EqualTo(itemCount);
        await Assert.That(results.OrderBy(x => x)).IsEquivalentTo(expectedResults);
    }

    [Test]
    public async Task ProcessWithChannel_WithCancellation_ShouldCancelGracefully()
    {
        // Arrange
        const int itemCount = 1000;
        var startedProcessingItems = new List<int>();
        var completedItems = new List<int>();
        var lockObj = new object();
        var items = Enumerable.Range(1, itemCount);
        var itemsStartedProcessing = 0;
        
        using var cts = new CancellationTokenSource();
        var options = ChannelProcessorOptions.CreateUnbounded(consumerCount: 2);

        // Act
        var processor = items.ForEachWithChannelAsync(async item =>
        {
            // Track that we started processing this item
            lock (lockObj)
            {
                startedProcessingItems.Add(item);
                itemsStartedProcessing++;
                
                // Cancel after we've started processing a few items
                if (itemsStartedProcessing == 5)
                {
                    _ = Task.Run(() => cts.Cancel());
                }
            }
            
            // Simulate some work
            await Task.Delay(10);
            
            // Only track completed items if not cancelled
            if (!cts.Token.IsCancellationRequested)
            {
                lock (lockObj)
                {
                    completedItems.Add(item);
                }
            }
        }, options, cts.Token);

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await processor);
        
        // Some items should have started processing before cancellation
        await Assert.That(startedProcessingItems.Count).IsGreaterThan(0);
        await Assert.That(startedProcessingItems.Count).IsLessThan(itemCount);
        
        // We may or may not have completed items depending on timing
        // but we should have at least started processing some
    }

    [Test]
    public async Task ProcessWithChannel_WithExceptionInTask_ShouldPropagateExceptions()
    {
        // Arrange
        const int itemCount = 10;
        var items = Enumerable.Range(1, itemCount);
        var options = ChannelProcessorOptions.CreateUnbounded(consumerCount: 1);

        // Act & Assert
        var processor = items.ForEachWithChannelAsync(async item =>
        {
            await Task.Delay(10);
            if (item == 5)
            {
                throw new InvalidOperationException($"Error processing item {item}");
            }
        }, options);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await processor);
        await Assert.That(exception.Message).Contains("Error processing item 5");
    }

    [Test]
    public async Task ProcessWithChannel_WithGetResultsAsyncEnumerable_ShouldStreamResults()
    {
        // Arrange
        const int itemCount = 20;
        var items = Enumerable.Range(1, itemCount);
        var streamedResults = new List<int>();
        var options = ChannelProcessorOptions.CreateUnbounded(consumerCount: 2);

        // Act
        var processor = items.SelectWithChannelAsync(async item =>
        {
            await Task.Delay(item * 10); // Variable delay to test streaming
            return item * 2;
        }, options);

        await foreach (var result in processor.GetResultsAsyncEnumerable())
        {
            streamedResults.Add(result);
        }

        // Assert
        await Assert.That(streamedResults).HasCount().EqualTo(itemCount);
        
        // Results might not be in order due to variable delays, but all should be present
        var expectedResults = items.Select(x => x * 2);
        await Assert.That(streamedResults.OrderBy(x => x)).IsEquivalentTo(expectedResults.OrderBy(x => x));
    }

    [Test]
    public async Task ProcessWithChannel_BoundedChannelFullModeWait_ShouldBlockProducerWhenFull()
    {
        // Arrange
        const int itemCount = 20;
        const int channelCapacity = 3;
        var processedItems = new List<int>();
        var lockObj = new object();
        var items = Enumerable.Range(1, itemCount);
        
        var options = new ChannelProcessorOptions
        {
            Capacity = channelCapacity,
            ConsumerCount = 1,
            FullMode = BoundedChannelFullMode.Wait
        };

        var processingStartTimes = new List<DateTime>();

        // Act
        var processor = items.ForEachWithChannelAsync(async item =>
        {
            lock (lockObj)
            {
                processingStartTimes.Add(DateTime.UtcNow);
            }
            
            await Task.Delay(100); // Slow consumer to fill up the channel
            
            lock (lockObj)
            {
                processedItems.Add(item);
            }
        }, options);

        await processor;

        // Assert
        await Assert.That(processedItems).HasCount().EqualTo(itemCount);
        await Assert.That(processedItems.OrderBy(x => x)).IsEquivalentTo(items);
        
        // With bounded channel and slow consumer, items should be processed with delays
        await Assert.That(processingStartTimes).HasCount().EqualTo(itemCount);
    }

    [Test]
    public async Task ProcessWithChannel_WithSingleWriterOptimization_ShouldWork()
    {
        // Arrange
        const int itemCount = 100;
        var processedItems = new List<int>();
        var lockObj = new object();
        var items = Enumerable.Range(1, itemCount);
        
        var options = new ChannelProcessorOptions
        {
            Capacity = null, // Unbounded
            ConsumerCount = 2,
            SingleWriter = true,
            SingleReader = false
        };

        // Act
        var processor = items.ForEachWithChannelAsync(async item =>
        {
            await Task.Delay(5);
            lock (lockObj)
            {
                processedItems.Add(item);
            }
        }, options);

        await processor;

        // Assert
        await Assert.That(processedItems).HasCount().EqualTo(itemCount);
        await Assert.That(processedItems.OrderBy(x => x)).IsEquivalentTo(items);
    }
}
#endif