#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.Example;

/// <summary>
/// Examples demonstrating channel-based processing capabilities
/// </summary>
public static class ChannelProcessingExamples
{
    /// <summary>
    /// Demonstrates basic channel-based processing with multiple consumers
    /// </summary>
    public static async Task BasicChannelProcessingExample()
    {
        Console.WriteLine("=== Basic Channel Processing Example ===");
        
        var urls = new[]
        {
            "https://api.example.com/data/1",
            "https://api.example.com/data/2", 
            "https://api.example.com/data/3",
            "https://api.example.com/data/4",
            "https://api.example.com/data/5",
            "https://api.example.com/data/6"
        };
        
        // Create channel options with multiple consumers
        var options = ChannelProcessorOptions.CreateUnbounded(consumerCount: 3);
        
        var processedCount = 0;
        
        // Process URLs with multiple consumers working in parallel
        var processor = urls.ForEachWithChannelAsync(async url =>
        {
            // Simulate HTTP request
            await Task.Delay(Random.Shared.Next(100, 500));
            
            var count = Interlocked.Increment(ref processedCount);
            Console.WriteLine($"[Consumer {Environment.CurrentManagedThreadId}] Processed {url} ({count}/{urls.Length})");
            
        }, options);
        
        var stopwatch = Stopwatch.StartNew();
        await processor;
        stopwatch.Stop();
        
        Console.WriteLine($"Processed {urls.Length} URLs in {stopwatch.ElapsedMilliseconds}ms using multiple consumers");
    }
    
    /// <summary>
    /// Demonstrates bounded channel with backpressure handling
    /// </summary>
    public static async Task BoundedChannelWithBackpressureExample()
    {
        Console.WriteLine("\n=== Bounded Channel with Backpressure Example ===");
        
        var documents = Enumerable.Range(1, 20).Select(i => $"Document_{i:D3}.pdf").ToArray();
        
        // Create bounded channel with limited capacity
        var options = ChannelProcessorOptions.CreateBounded(
            capacity: 3,           // Small buffer to demonstrate backpressure
            consumerCount: 1       // Single slow consumer
        );
        
        var processedDocs = new List<string>();
        var lockObj = new object();
        
        var processor = documents.ForEachWithChannelAsync(async doc =>
        {
            // Simulate slow document processing
            Console.WriteLine($"Processing {doc}...");
            await Task.Delay(200);
            
            lock (lockObj)
            {
                processedDocs.Add(doc);
            }
            
            Console.WriteLine($"✓ Completed {doc} ({processedDocs.Count}/{documents.Length})");
            
        }, options);
        
        var stopwatch = Stopwatch.StartNew();
        await processor;
        stopwatch.Stop();
        
        Console.WriteLine($"Processed {documents.Length} documents in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine("Note: Producer was throttled by bounded channel capacity");
    }
    
    /// <summary>
    /// Demonstrates result processing with streaming output
    /// </summary>
    public static async Task ResultProcessingWithStreamingExample()
    {
        Console.WriteLine("\n=== Result Processing with Streaming Example ===");
        
        var numbers = Enumerable.Range(1, 10).ToArray();
        
        // Process numbers and return results using multiple consumers
        var options = ChannelProcessorOptions.CreateUnbounded(consumerCount: 4);
        
        var processor = numbers.SelectWithChannelAsync(async number =>
        {
            // Simulate CPU-intensive calculation
            await Task.Delay(Random.Shared.Next(50, 200));
            
            var result = Math.Pow(number, 3); // Calculate cube
            Console.WriteLine($"[Consumer {Environment.CurrentManagedThreadId}] {number}³ = {result}");
            
            return new { Number = number, Cube = result };
            
        }, options);
        
        Console.WriteLine("Streaming results as they become available:");
        
        var results = new List<object>();
        await foreach (var result in processor.GetResultsAsyncEnumerable())
        {
            results.Add(result);
            Console.WriteLine($"Got result: {result.Number}³ = {result.Cube}");
        }
        
        Console.WriteLine($"Total results received: {results.Count}");
    }
    
    /// <summary>
    /// Demonstrates channel options configuration
    /// </summary>
    public static async Task ChannelOptionsConfigurationExample()
    {
        Console.WriteLine("\n=== Channel Options Configuration Example ===");
        
        var tasks = Enumerable.Range(1, 15);
        
        // Configure channel with specific options
        var options = new ChannelProcessorOptions
        {
            Capacity = 5,                                    // Bounded channel
            FullMode = BoundedChannelFullMode.Wait,         // Block producer when full
            ConsumerCount = 3,                              // Multiple consumers
            SingleWriter = true,                            // Optimize for single producer
            SingleReader = false,                           // Multiple readers
            AllowSynchronousContinuations = false          // Use thread pool for continuations
        };
        
        var processedTasks = new List<int>();
        var lockObj = new object();
        
        var processor = tasks.ForEachWithChannelAsync(async task =>
        {
            Console.WriteLine($"[Consumer {Environment.CurrentManagedThreadId}] Starting task {task}");
            await Task.Delay(100);
            
            lock (lockObj)
            {
                processedTasks.Add(task);
            }
            
            Console.WriteLine($"[Consumer {Environment.CurrentManagedThreadId}] Completed task {task}");
            
        }, options);
        
        await processor;
        
        Console.WriteLine($"Processed {processedTasks.Count} tasks with custom channel configuration");
        Console.WriteLine($"Tasks processed: [{string.Join(", ", processedTasks.OrderBy(x => x))}]");
    }
    
    /// <summary>
    /// Performance comparison between channel-based and batch processing
    /// </summary>
    public static async Task PerformanceComparisonExample()
    {
        Console.WriteLine("\n=== Performance Comparison Example ===");
        
        var workItems = Enumerable.Range(1, 200).ToArray();
        
        // Test channel-based processing
        Console.WriteLine("Testing Channel-based processing...");
        var channelOptions = ChannelProcessorOptions.CreateUnbounded(consumerCount: 4);
        var channelStopwatch = Stopwatch.StartNew();
        
        var channelProcessor = workItems.ForEachWithChannelAsync(async item =>
        {
            await Task.Delay(5); // Simulate work
        }, channelOptions);
        
        await channelProcessor;
        channelStopwatch.Stop();
        
        // Test traditional batch processing
        Console.WriteLine("Testing Batch processing...");
        var batchStopwatch = Stopwatch.StartNew();
        
        var batchProcessor = workItems.ForEachAsync(async item =>
        {
            await Task.Delay(5); // Simulate work
        }).ProcessInBatches(4);
        
        await batchProcessor;
        batchStopwatch.Stop();
        
        // Compare results
        Console.WriteLine($"Channel-based: {channelStopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Batch-based:   {batchStopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Performance ratio: {(double)channelStopwatch.ElapsedMilliseconds / batchStopwatch.ElapsedMilliseconds:F2}x");
        
        if (channelStopwatch.ElapsedMilliseconds < batchStopwatch.ElapsedMilliseconds * 1.2)
        {
            Console.WriteLine("✓ Channel-based processing is competitive with batch processing");
        }
    }
    
    /// <summary>
    /// Run all channel processing examples
    /// </summary>
    public static async Task RunAllExamples()
    {
        await BasicChannelProcessingExample();
        await BoundedChannelWithBackpressureExample();
        await ResultProcessingWithStreamingExample();
        await ChannelOptionsConfigurationExample();
        await PerformanceComparisonExample();
        
        Console.WriteLine("\n=== All Channel Processing Examples Completed ===");
    }
}
#endif