#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.Example;

public static class ProcessInParallelExample
{
    public static async Task RunExample()
    {
        Console.WriteLine("ProcessInParallel Extension Examples");
        Console.WriteLine("====================================\n");
        
        // Example 1: Simple parallel processing without transformation
        Console.WriteLine("Example 1: Simple parallel processing (no transformation needed!)");
        var asyncEnumerable1 = GenerateAsyncEnumerable(5);
        IEnumerable<int> results1 = await asyncEnumerable1.ProcessInParallel();  // <-- This is the simple extension!
        Console.WriteLine($"Results: {string.Join(", ", results1)}");
        
        // Example 2: Parallel processing with transformation
        Console.WriteLine("\nExample 2: Parallel processing with transformation");
        var asyncEnumerable2 = GenerateAsyncEnumerable(5);
        var results2 = await asyncEnumerable2.ProcessInParallel(
            async item =>
            {
                await Task.Delay(100); // Simulate async work
                return item * 2;
            });
        Console.WriteLine($"Transformed results: {string.Join(", ", results2)}");
        
        // Example 3: Parallel processing with max concurrency
        Console.WriteLine("\nExample 3: Parallel processing with max concurrency (3)");
        var asyncEnumerable3 = GenerateAsyncEnumerable(10);
        var results3 = await asyncEnumerable3.ProcessInParallel(
            async item =>
            {
                Console.WriteLine($"Processing item {item}");
                await Task.Delay(100);
                return $"Item-{item}";
            },
            maxConcurrency: 3);
        Console.WriteLine($"Results count: {results3.Count()}");
        
        // Example 4: Performance comparison
        Console.WriteLine("\nExample 4: Performance comparison");
        var itemCount = 20;
        var asyncEnumerable4 = GenerateAsyncEnumerable(itemCount);
        
        var start = DateTime.Now;
        var sequentialResults = new List<int>();
        await foreach (var item in asyncEnumerable4)
        {
            await Task.Delay(50); // Simulate work
            sequentialResults.Add(item * 2);
        }
        var sequentialTime = DateTime.Now - start;
        
        var asyncEnumerable5 = GenerateAsyncEnumerable(itemCount);
        start = DateTime.Now;
        var parallelResults = await asyncEnumerable5.ProcessInParallel(
            async item =>
            {
                await Task.Delay(50); // Simulate work
                return item * 2;
            });
        var parallelTime = DateTime.Now - start;
        
        Console.WriteLine($"Sequential processing time: {sequentialTime.TotalMilliseconds:F0}ms");
        Console.WriteLine($"Parallel processing time: {parallelTime.TotalMilliseconds:F0}ms");
        Console.WriteLine($"Speedup: {sequentialTime.TotalMilliseconds / parallelTime.TotalMilliseconds:F1}x");
    }
    
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