#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.Example;

/// <summary>
/// Examples demonstrating proper disposal patterns for EnumerableAsyncProcessor objects.
/// This addresses the common question: "How/when to correctly dispose the resulting processor objects?"
/// 
/// QUICK ANSWER: Always dispose processor objects using 'await using' or manual disposal!
/// 
/// The key pattern is:
/// ❌ BAD:  var processor = items.SelectAsync(...).ProcessInParallel(); // Never disposed!
/// ✅ GOOD: await using var processor = items.SelectAsync(...).ProcessInParallel(); // Auto-disposed!
/// </summary>
public static class DisposalExample
{
    public static async Task RunExamples()
    {
        Console.WriteLine("Disposal Pattern Examples");
        Console.WriteLine("========================\n");
        
        // Example 1: The problematic pattern from the issue
        Console.WriteLine("Example 1: PROBLEMATIC - No disposal (resource leak!)");
        var results1 = await ProblematicPatternAsync(new[] { 1, 2, 3, 4, 5 }, CancellationToken.None);
        Console.WriteLine($"Results: {string.Join(", ", results1.ToList())}");
        Console.WriteLine("⚠️  This pattern leaks resources because the processor is never disposed!\n");
        
        // Example 2: Proper disposal with await using
        Console.WriteLine("Example 2: PROPER - Using await using for automatic disposal");
        var results2 = await ProperPatternWithAwaitUsingAsync(new[] { 1, 2, 3, 4, 5 }, CancellationToken.None);
        Console.WriteLine($"Results: {string.Join(", ", results2.ToList())}");
        Console.WriteLine("✅ Resources automatically cleaned up with await using\n");
        
        // Example 3: Proper disposal with manual try-finally
        Console.WriteLine("Example 3: PROPER - Manual disposal with try-finally");
        var results3 = await ProperPatternWithManualDisposalAsync(new[] { 1, 2, 3, 4, 5 }, CancellationToken.None);
        Console.WriteLine($"Results: {string.Join(", ", results3.ToList())}");
        Console.WriteLine("✅ Resources manually cleaned up in finally block\n");
        
        // Example 4: Using the convenience extension (no disposal needed)
        Console.WriteLine("Example 4: CONVENIENT - Using extension methods (disposal handled internally)");
        var asyncEnumerable = GenerateAsyncEnumerable(5);
        var results4 = await asyncEnumerable.ProcessInParallel(async item =>
        {
            await Task.Delay(50);
            return item * 2;
        });
        Console.WriteLine($"Results: {string.Join(", ", results4)}");
        Console.WriteLine("✅ Extension methods handle disposal internally\n");
        
        // Example 5: Streaming results with proper disposal
        Console.WriteLine("Example 5: STREAMING - Processing results as they arrive with proper disposal");
        await StreamingWithProperDisposalAsync(new[] { 1, 2, 3, 4, 5 }, CancellationToken.None);
        Console.WriteLine("✅ Streamed results with proper disposal\n");
    }
    
    /// <summary>
    /// This is the PROBLEMATIC pattern from the GitHub issue - it leaks resources!
    /// DO NOT USE THIS PATTERN in production code.
    /// </summary>
    private static async Task<IAsyncEnumerable<int>> ProblematicPatternAsync(int[] input, CancellationToken token)
    {
        // ⚠️ PROBLEM: The processor is created but never disposed!
        var batchProcessor = input.SelectAsync(static v => TransformAsync(v), token).ProcessInParallel();
        
        // This returns the async enumerable, but the processor that created it is never disposed
        return batchProcessor.GetResultsAsyncEnumerable();
        
        // 🔥 RESOURCE LEAK: The processor goes out of scope without being disposed,
        // potentially leaving tasks running and resources uncleaned
    }
    
    /// <summary>
    /// PROPER pattern using await using for automatic disposal.
    /// This is the recommended approach.
    /// </summary>
    private static async Task<IAsyncEnumerable<int>> ProperPatternWithAwaitUsingAsync(int[] input, CancellationToken token)
    {
        // ✅ Create processor with await using for automatic disposal
        await using var processor = input.SelectAsync(static v => TransformAsync(v), token).ProcessInParallel();
        
        // Collect results into a list to return
        var results = new List<int>();
        await foreach (var result in processor.GetResultsAsyncEnumerable())
        {
            results.Add(result);
        }
        
        // Return as async enumerable
        return results.ToAsyncEnumerable();
        
        // ✅ Processor is automatically disposed here due to 'await using'
    }
    
    /// <summary>
    /// PROPER pattern using manual disposal with try-finally.
    /// Use this when you need more control over the disposal timing.
    /// </summary>
    private static async Task<IAsyncEnumerable<int>> ProperPatternWithManualDisposalAsync(int[] input, CancellationToken token)
    {
        var processor = input.SelectAsync(static v => TransformAsync(v), token).ProcessInParallel();
        
        try
        {
            // Collect results into a list to return
            var results = new List<int>();
            await foreach (var result in processor.GetResultsAsyncEnumerable())
            {
                results.Add(result);
            }
            
            return results.ToAsyncEnumerable();
        }
        finally
        {
            // ✅ Manually dispose the processor to clean up resources
            await processor.DisposeAsync();
        }
    }
    
    /// <summary>
    /// Example of streaming results while maintaining proper disposal.
    /// This shows how to process results as they arrive.
    /// </summary>
    private static async Task StreamingWithProperDisposalAsync(int[] input, CancellationToken token)
    {
        await using var processor = input.SelectAsync(static v => TransformAsync(v), token).ProcessInParallel();
        
        var processedCount = 0;
        await foreach (var result in processor.GetResultsAsyncEnumerable())
        {
            processedCount++;
            Console.WriteLine($"  Received result {processedCount}: {result}");
        }
        
        // Processor automatically disposed here
    }
    
    /// <summary>
    /// Simulates an async transformation operation
    /// </summary>
    private static async Task<int> TransformAsync(int value)
    {
        // Simulate some async work
        await Task.Delay(50);
        return value * 10;
    }
    
    /// <summary>
    /// Generates an async enumerable for testing
    /// </summary>
    private static async IAsyncEnumerable<int> GenerateAsyncEnumerable(
        int count, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 1; i <= count; i++)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return i;
        }
    }
}
#endif