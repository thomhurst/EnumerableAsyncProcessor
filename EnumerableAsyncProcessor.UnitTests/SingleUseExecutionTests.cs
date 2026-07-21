using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;
using TUnit.Assertions;
using TUnit.Core;

namespace EnumerableAsyncProcessor.UnitTests;

/// <summary>
/// Guards the single-use contract of the IAsyncEnumerable processors (issue #369):
/// a second ExecuteAsync call must throw InvalidOperationException naming the contract,
/// and ExecuteAsync after disposal must throw ObjectDisposedException naming the processor -
/// never a bare ObjectDisposedException from the internal CancellationTokenSource.
/// </summary>
public class SingleUseExecutionTests
{
    private static async IAsyncEnumerable<int> Source(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }

    [Test]
    public async Task Void_Processor_Second_ExecuteAsync_Throws_InvalidOperationException()
    {
        var processor = Source(3).ForEachAsync(_ => Task.CompletedTask).ProcessInParallel(2);
        await processor.ExecuteAsync();

        var caught = Catch(() => processor.ExecuteAsync());

        await Assert.That(caught is InvalidOperationException).IsTrue();
        await Assert.That(caught!.Message).Contains("single-use");
    }

    [Test]
    public async Task Result_Processor_Second_ExecuteAsync_Throws_InvalidOperationException_Eagerly()
    {
        var processor = Source(3).SelectAsync(i => Task.FromResult(i)).ProcessInParallel(2);

        await foreach (var _ in processor.ExecuteAsync())
        {
        }

        // The guard must fire at the ExecuteAsync call, not on first MoveNextAsync
        var caught = Catch(() => processor.ExecuteAsync());

        await Assert.That(caught is InvalidOperationException).IsTrue();
        await Assert.That(caught!.Message).Contains("single-use");
    }

    [Test]
    public async Task Void_Processor_ExecuteAsync_After_Dispose_Throws_ObjectDisposedException()
    {
        var processor = Source(3).ForEachAsync(_ => Task.CompletedTask).ProcessInParallel(2);
        processor.Dispose();

        var caught = Catch(() => processor.ExecuteAsync());

        await Assert.That(caught is ObjectDisposedException).IsTrue();
        await Assert.That(caught!.Message).Contains("AsyncEnumerableParallelProcessor");
    }

    [Test]
    public async Task Result_Processor_ExecuteAsync_After_Dispose_Throws_ObjectDisposedException()
    {
        var processor = Source(3).SelectAsync(i => Task.FromResult(i)).ProcessInParallel(2);
        await processor.DisposeAsync();

        var caught = Catch(() => processor.ExecuteAsync());

        await Assert.That(caught is ObjectDisposedException).IsTrue();
        await Assert.That(caught!.Message).Contains("ResultAsyncEnumerableParallelProcessor");
    }

    [Test]
    public async Task Batch_And_OneAtATime_Processors_Enforce_Single_Use()
    {
        var batchProcessor = Source(3).ForEachAsync(_ => Task.CompletedTask).ProcessInBatches(2);
        await batchProcessor.ExecuteAsync();
        await Assert.That(Catch(() => batchProcessor.ExecuteAsync()) is InvalidOperationException).IsTrue();

        var oneAtATimeProcessor = Source(3).SelectAsync(i => Task.FromResult(i)).ProcessOneAtATime();
        await foreach (var _ in oneAtATimeProcessor.ExecuteAsync())
        {
        }

        await Assert.That(Catch(() => oneAtATimeProcessor.ExecuteAsync()) is InvalidOperationException).IsTrue();
    }

    private static Exception? Catch(Func<object> action)
    {
        try
        {
            _ = action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
