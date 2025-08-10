using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.Example;

public static class AsyncEnumerableExample
{
    public static async Task RunExamples()
    {
        Console.WriteLine("=== IAsyncEnumerable Parallel Processing Examples ===\n");

        // Example 1: ForEachAsync with parallel processing
        Console.WriteLine("Example 1: Processing async enumerable items in parallel");
        var items = GenerateAsyncNumbers(10);
        
        await items
            .ForEachAsync(async number =>
            {
                await Task.Delay(100); // Simulate I/O work
                Console.WriteLine($"Processed {number} on thread {Thread.CurrentThread.ManagedThreadId}");
            })
            .ProcessInParallel(3)
            .ExecuteAsync(); // Process with max 3 concurrent tasks

        Console.WriteLine("\nExample 2: SelectAsync with transformation");
        var transformedItems = GenerateAsyncNumbers(5);
        
        var results = await transformedItems
            .SelectAsync(async number =>
            {
                await Task.Delay(50);
                return number * 2;
            })
            .ProcessInParallel(2)
            .ExecuteAsync()
            .ToListAsync();

        Console.WriteLine($"Transformed results: {string.Join(", ", results)}");

        // Example 3: High concurrency for I/O-bound operations
        Console.WriteLine("\nExample 3: High concurrency I/O operations");
        var ioItems = GenerateAsyncNumbers(20);
        
        await ioItems
            .ForEachAsync(async number =>
            {
                await SimulateApiCall(number);
                Console.WriteLine($"API call {number} completed");
            })
            .ProcessInParallel(10)
            .ExecuteAsync(); // Process with controlled concurrency

        Console.WriteLine("\nAll examples completed!");
    }

    private static async IAsyncEnumerable<int> GenerateAsyncNumbers(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            await Task.Yield(); // Simulate async generation
            yield return i;
        }
    }

    private static async Task SimulateApiCall(int id)
    {
        await Task.Delay(Random.Shared.Next(10, 50));
    }

    private static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source)
        {
            list.Add(item);
        }
        return list;
    }
}