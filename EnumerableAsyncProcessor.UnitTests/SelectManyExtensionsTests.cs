using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;
using TUnit.Assertions;
using TUnit.Core;

namespace EnumerableAsyncProcessor.UnitTests;

public class SelectManyExtensionsTests
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

    private static async IAsyncEnumerable<int> GenerateAsyncRange(int start, int count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = start; i < start + count; i++)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return i;
        }
    }

    [Test]
    public async Task SelectMany_IAsyncEnumerable_WithIEnumerable_FlattensResults()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(3);
        
        var results = new List<int>();
        await foreach (var item in asyncEnumerable.SelectMany(x => Enumerable.Range(x * 10, 3)))
        {
            results.Add(item);
        }
        
        // Input: [1, 2, 3]
        // Output: [10,11,12, 20,21,22, 30,31,32]
        await Assert.That(results).IsEquivalentTo(new[] { 10, 11, 12, 20, 21, 22, 30, 31, 32 });
    }

    [Test]
    public async Task SelectMany_IAsyncEnumerable_WithIAsyncEnumerable_FlattensResults()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(3);
        
        var results = new List<int>();
        await foreach (var item in asyncEnumerable.SelectMany(x => GenerateAsyncRange(x * 10, 2)))
        {
            results.Add(item);
        }
        
        // Input: [1, 2, 3]
        // Output: [10,11, 20,21, 30,31]
        await Assert.That(results).IsEquivalentTo(new[] { 10, 11, 20, 21, 30, 31 });
    }

    [Test]
    public async Task SelectManyAsync_IAsyncEnumerable_WithTaskIEnumerable_FlattensResults()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(3);
        
        var results = new List<string>();
        await foreach (var item in asyncEnumerable.SelectManyAsync(async x =>
        {
            await Task.Delay(10);
            return Enumerable.Range(x * 10, 2).Select(n => n.ToString());
        }))
        {
            results.Add(item);
        }
        
        // Input: [1, 2, 3]
        // Output: ["10","11", "20","21", "30","31"]
        await Assert.That(results).IsEquivalentTo(new[] { "10", "11", "20", "21", "30", "31" });
    }

    [Test]
    public async Task SelectManyAsync_IAsyncEnumerable_WithTaskIAsyncEnumerable_FlattensResults()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(2);
        
        var results = new List<int>();
        await foreach (var item in asyncEnumerable.SelectManyAsync(async x =>
        {
            await Task.Delay(10);
            return GenerateAsyncRange(x * 100, 3);
        }))
        {
            results.Add(item);
        }
        
        // Input: [1, 2]
        // Output: [100,101,102, 200,201,202]
        await Assert.That(results).IsEquivalentTo(new[] { 100, 101, 102, 200, 201, 202 });
    }

    [Test]
    public async Task SelectManyAsync_IEnumerable_WithIAsyncEnumerable_FlattensResults()
    {
        var enumerable = Enumerable.Range(1, 3);
        
        var results = new List<int>();
        await foreach (var item in enumerable.SelectManyAsync(x => GenerateAsyncRange(x * 10, 2)))
        {
            results.Add(item);
        }
        
        // Input: [1, 2, 3]
        // Output: [10,11, 20,21, 30,31]
        await Assert.That(results).IsEquivalentTo(new[] { 10, 11, 20, 21, 30, 31 });
    }

    [Test]
    public async Task SelectManyAsync_IEnumerable_WithTaskIEnumerable_FlattensResults()
    {
        var enumerable = Enumerable.Range(1, 3);
        
        var results = new List<string>();
        await foreach (var item in enumerable.SelectManyAsync(async x =>
        {
            await Task.Delay(10);
            return new[] { $"A{x}", $"B{x}" };
        }))
        {
            results.Add(item);
        }
        
        // Input: [1, 2, 3]
        // Output: ["A1","B1", "A2","B2", "A3","B3"]
        await Assert.That(results).IsEquivalentTo(new[] { "A1", "B1", "A2", "B2", "A3", "B3" });
    }

    [Test]
    public async Task SelectManyAsync_IEnumerable_WithTaskIAsyncEnumerable_FlattensResults()
    {
        var enumerable = Enumerable.Range(1, 2);
        
        var results = new List<int>();
        await foreach (var item in enumerable.SelectManyAsync(async x =>
        {
            await Task.Delay(10);
            return GenerateAsyncRange(x * 100, 3);
        }))
        {
            results.Add(item);
        }
        
        // Input: [1, 2]
        // Output: [100,101,102, 200,201,202]
        await Assert.That(results).IsEquivalentTo(new[] { 100, 101, 102, 200, 201, 202 });
    }

    [Test]
    public async Task SelectMany_WithEmptySubCollections_HandlesCorrectly()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(3);
        
        var results = new List<int>();
        await foreach (var item in asyncEnumerable.SelectMany(x => 
            x == 2 ? Enumerable.Empty<int>() : new[] { x * 10 }))
        {
            results.Add(item);
        }
        
        // Input: [1, 2, 3]
        // Output: [10, 30] (2 produces empty)
        await Assert.That(results).IsEquivalentTo(new[] { 10, 30 });
    }

    [Test]
    public async Task SelectMany_WithCancellation_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        var asyncEnumerable = GenerateAsyncEnumerable(100);
        
        cts.CancelAfter(50);
        
        var results = new List<int>();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in asyncEnumerable.SelectMany(
                x => Enumerable.Range(x * 10, 10), cts.Token))
            {
                results.Add(item);
                await Task.Delay(10);
            }
        });
    }

    [Test]
    public async Task SelectManyAsync_HandlesExceptions()
    {
        var asyncEnumerable = GenerateAsyncEnumerable(3);
        
        var results = asyncEnumerable.SelectManyAsync(async x =>
        {
            if (x == 2)
            {
                throw new InvalidOperationException("Test exception");
            }
            await Task.Delay(10);
            return new[] { x * 10 };
        });
        
        var items = new List<int>();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var item in results)
            {
                items.Add(item);
            }
        });
        
        // Should have processed first item before exception
        await Assert.That(items).IsEquivalentTo(new[] { 10 });
    }

    [Test]
    public async Task SelectMany_ComplexNesting_WorksCorrectly()
    {
        // Test a more complex scenario with nested SelectMany using LINQ's built-in
        var data = new[] { 1, 2 };
        
        var results = System.Linq.Enumerable.SelectMany(data, x => 
            System.Linq.Enumerable.SelectMany(Enumerable.Range(1, 2), y => 
                new[] { $"{x}-{y}-A", $"{x}-{y}-B" })).ToList();
        
        await Assert.That(results).IsEquivalentTo(new[] 
        { 
            "1-1-A", "1-1-B", "1-2-A", "1-2-B",
            "2-1-A", "2-1-B", "2-2-A", "2-2-B"
        });
    }
}