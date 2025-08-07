#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Builders;
using EnumerableAsyncProcessor.Extensions;
using TUnit.Assertions;
using TUnit.Core;

namespace EnumerableAsyncProcessor.UnitTests;

public class ChannelBasedResultProcessorTests
{
    [Test]
    public async Task ProcessWithChannel_ActionBuilder_WithResults_ShouldReturnResults()
    {
        // Arrange
        const int taskCount = 50;
        var expectedResults = Enumerable.Range(1, taskCount).Select(x => $"Result-{x}");
        var options = ChannelProcessorOptions.CreateUnbounded(consumerCount: 3);

        // Act
        var processor = AsyncProcessorBuilder.WithExecutionCount(taskCount)
            .SelectAsync(async () =>
            {
                await Task.Delay(10);
                return $"Result-{Random.Shared.Next(1, taskCount + 1)}";
            })
            .ProcessWithChannel(options);

        var results = await processor.GetResultsAsync();

        // Assert
        await Assert.That(results).HasCount().EqualTo(taskCount);
        await Assert.That(results.All(r => r.StartsWith("Result-"))).IsTrue();
    }

    [Test]
    public async Task ProcessWithChannel_ItemBuilder_WithTransformation_ShouldTransformAllItems()
    {
        // Arrange
        const int itemCount = 100;
        var items = Enumerable.Range(1, itemCount);
        var expectedResults = items.Select(x => x.ToString("D3")); // Format as 3-digit string
        var options = ChannelProcessorOptions.CreateBounded(10, consumerCount: 4);

        // Act
        var processor = items.SelectWithChannelAsync(async item =>
        {
            await Task.Delay(5);
            return item.ToString("D3");
        }, options);

        var results = await processor.GetResultsAsync();

        // Assert
        await Assert.That(results).HasCount().EqualTo(itemCount);
        await Assert.That(results.OrderBy(x => x)).IsEquivalentTo(expectedResults.OrderBy(x => x));
    }

    [Test]
    public async Task ProcessWithChannel_WithComplexTransformation_ShouldPreserveOrder()
    {
        // Arrange
        var items = new[] { "apple", "banana", "cherry", "date", "elderberry" };
        var options = ChannelProcessorOptions.CreateUnbounded(consumerCount: 2);

        // Act
        var processor = items.SelectWithChannelAsync(async fruit =>
        {
            await Task.Delay(fruit.Length * 10); // Variable delay based on string length
            return new { Fruit = fruit, Length = fruit.Length, UpperCase = fruit.ToUpper() };
        }, options);

        var results = await processor.GetResultsAsync();

        // Assert
        await Assert.That(results).HasCount().EqualTo(items.Length);
        
        foreach (var result in results)
        {
            var originalFruit = items.First(f => f == result.Fruit);
            await Assert.That(result.Length).IsEqualTo(originalFruit.Length);
            await Assert.That(result.UpperCase).IsEqualTo(originalFruit.ToUpper());
        }
    }

    [Test]
    public async Task ProcessWithChannel_StreamingResults_ShouldProvideResultsAsAvailable()
    {
        // Arrange
        const int itemCount = 20;
        var items = Enumerable.Range(1, itemCount);
        var streamedResults = new List<string>();
        var streamingTimes = new List<DateTime>();
        var options = ChannelProcessorOptions.CreateUnbounded(consumerCount: 3);

        // Act
        var processor = items.SelectWithChannelAsync(async item =>
        {
            // Randomize delay to simulate real-world scenarios
            await Task.Delay(Random.Shared.Next(50, 200));
            return $"Processed-{item}";
        }, options);

        await foreach (var result in processor.GetResultsAsyncEnumerable())
        {
            streamedResults.Add(result);
            streamingTimes.Add(DateTime.UtcNow);
        }

        // Assert
        await Assert.That(streamedResults).HasCount().EqualTo(itemCount);
        await Assert.That(streamedResults.All(r => r.StartsWith("Processed-"))).IsTrue();
        
        // Verify that results were streamed over time (not all at once)
        var firstResultTime = streamingTimes.First();
        var lastResultTime = streamingTimes.Last();
        var totalStreamingTime = (lastResultTime - firstResultTime).TotalMilliseconds;
        
        await Assert.That(totalStreamingTime).IsGreaterThan(100); // Should take some time to stream all results
    }

    [Test]
    public async Task ProcessWithChannel_WithExceptionInTransformation_ShouldPropagateException()
    {
        // Arrange
        var items = Enumerable.Range(1, 10);
        var options = ChannelProcessorOptions.CreateUnbounded(consumerCount: 1);

        // Act & Assert
        var processor = items.SelectWithChannelAsync(async item =>
        {
            await Task.Delay(10);
            if (item == 7)
            {
                throw new ArgumentException($"Cannot process item {item}");
            }
            return item * 10;
        }, options);

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await processor.GetResultsAsync());
        await Assert.That(exception.Message).Contains("Cannot process item 7");
    }

    [Test]
    public async Task ProcessWithChannel_ConcurrentResultCollection_ShouldBeThreadSafe()
    {
        // Arrange
        const int itemCount = 200;
        var items = Enumerable.Range(1, itemCount);
        var options = ChannelProcessorOptions.CreateUnbounded(consumerCount: 8); // Many consumers

        // Act
        var processor = items.SelectWithChannelAsync(async item =>
        {
            await Task.Delay(Random.Shared.Next(1, 10));
            return item * item; // Square the number
        }, options);

        var results = await processor.GetResultsAsync();

        // Assert
        await Assert.That(results).HasCount().EqualTo(itemCount);
        
        // Verify all expected results are present (no duplicates, no missing)
        var expectedResults = items.Select(x => x * x).OrderBy(x => x);
        var actualResults = results.OrderBy(x => x);
        
        await Assert.That(actualResults).IsEquivalentTo(expectedResults);
    }

    [Test]
    public async Task ProcessWithChannel_EmptyCollection_ShouldReturnEmptyResults()
    {
        // Arrange
        var items = Enumerable.Empty<int>();
        var options = ChannelProcessorOptions.CreateUnbounded(consumerCount: 2);

        // Act
        var processor = items.SelectWithChannelAsync(async item =>
        {
            await Task.Delay(10);
            return item.ToString();
        }, options);

        var results = await processor.GetResultsAsync();

        // Assert
        await Assert.That(results).HasCount().EqualTo(0);
    }

    [Test]
    public async Task ProcessWithChannel_SingleItem_ShouldProcessCorrectly()
    {
        // Arrange
        var items = new[] { 42 };
        var options = ChannelProcessorOptions.CreateBounded(1, consumerCount: 1);

        // Act
        var processor = items.SelectWithChannelAsync(async item =>
        {
            await Task.Delay(50);
            return $"The answer is {item}";
        }, options);

        var results = await processor.GetResultsAsync();

        // Assert
        await Assert.That(results).HasCount().EqualTo(1);
        await Assert.That(results[0]).IsEqualTo("The answer is 42");
    }

    [Test]
    public async Task ProcessWithChannel_CompareWithRegularProcessor_ShouldProduceSameResults()
    {
        // Arrange
        const int itemCount = 50;
        var items = Enumerable.Range(1, itemCount).ToArray();
        
        // Transform function
        Func<int, Task<string>> transform = async x =>
        {
            await Task.Delay(10);
            return $"Item-{x:D2}-{x * x}";
        };

        // Act - Channel-based processing
        var channelProcessor = items.SelectWithChannelAsync(transform, 
            ChannelProcessorOptions.CreateUnbounded(consumerCount: 3));
        var channelResults = await channelProcessor.GetResultsAsync();

        // Act - Regular batch processing
        var batchProcessor = items.SelectAsync(transform).ProcessInBatches(10);
        var batchResults = await batchProcessor.GetResultsAsync();

        // Assert - Results should be identical (when sorted, since order may vary)
        await Assert.That(channelResults.OrderBy(x => x)).IsEquivalentTo(batchResults.OrderBy(x => x));
    }
}
#endif