using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.Example;

public static class SelectManyExample
{
    private static async IAsyncEnumerable<int> GenerateAsyncEnumerable(int count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 1; i <= count; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }

    public static async Task RunExample()
    {
        Console.WriteLine("SelectMany Extension Examples");
        Console.WriteLine("=============================\n");
        
        // Example 1: SelectMany on IAsyncEnumerable with IEnumerable
        Console.WriteLine("Example 1: IAsyncEnumerable.SelectMany with IEnumerable:");
        var asyncEnum = GenerateAsyncEnumerable(3);
        var results1 = new List<int>();
        await foreach (var item in asyncEnum.SelectMany(x => Enumerable.Range(x * 10, 2)))
        {
            results1.Add(item);
        }
        Console.WriteLine($"Input: [1, 2, 3] -> Output: [{string.Join(", ", results1)}]");
        
        // Example 2: SelectManyAsync on IAsyncEnumerable with Task<IEnumerable>
        Console.WriteLine("\nExample 2: IAsyncEnumerable.SelectManyAsync with Task<IEnumerable>:");
        var asyncEnum2 = GenerateAsyncEnumerable(3);
        var results2 = new List<int>();
        await foreach (var item in asyncEnum2.SelectManyAsync(async x =>
        {
            await Task.Delay(10);
            return new[] { x * 100, x * 100 + 1 };
        }))
        {
            results2.Add(item);
        }
        Console.WriteLine($"Input: [1, 2, 3] -> Output: [{string.Join(", ", results2)}]");
        
        // Example 3: SelectManyAsync on IEnumerable with IAsyncEnumerable
        Console.WriteLine("\nExample 3: IEnumerable.SelectManyAsync with IAsyncEnumerable:");
        var enumerable = Enumerable.Range(1, 3);
        var results3 = new List<int>();
        await foreach (var item in enumerable.SelectManyAsync(x => GenerateAsyncEnumerable(2)))
        {
            results3.Add(item);
        }
        Console.WriteLine($"Input: [1, 2, 3] -> Output: [{string.Join(", ", results3)}]");
        
        // Example 4: Flattening nested collections
        Console.WriteLine("\nExample 4: Flattening nested async collections:");
        var categories = new[] { "A", "B" };
        var results4 = new List<string>();
        await foreach (var item in categories.SelectManyAsync(async cat =>
        {
            await Task.Delay(10); // Simulate async work
            return Enumerable.Range(1, 3).Select(n => $"{cat}{n}").ToArray();
        }))
        {
            results4.Add(item);
        }
        Console.WriteLine($"Categories: [A, B] -> Flattened: [{string.Join(", ", results4)}]");
    }
}