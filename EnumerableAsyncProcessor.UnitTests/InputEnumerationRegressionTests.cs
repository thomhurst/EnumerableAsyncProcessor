using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.UnitTests;

/// <summary>
/// Guards against the input enumerable being lazily re-enumerated by the processor internals.
/// Historically the input was enumerated up to five times (validation, processing, awaiting,
/// cancellation and disposal), which broke one-shot sources and re-ran side effects.
/// </summary>
public class InputEnumerationRegressionTests
{
    [Test]
    public async Task Input_Enumerable_Is_Enumerated_Exactly_Once()
    {
        var source = new TrackingEnumerable(20);

        await using (var processor = source.ForEachAsync(_ => Task.CompletedTask).ProcessInParallel(4))
        {
            await processor.WaitAsync();

            // These all re-enumerated the input in previous versions
            _ = processor.GetEnumerableTasks().ToList();
            _ = processor.GetEnumerableTasks().ToList();
            processor.CancelAll();
        }

        await Assert.That(source.EnumerationCount).IsEqualTo(1);
        await Assert.That(source.ItemsYielded).IsEqualTo(20);
    }

    [Test]
    public async Task OneShot_Enumerable_Processes_All_Items()
    {
        var source = new OneShotEnumerable<int>(Enumerable.Range(0, 10));

        await using var processor = source.SelectAsync(i => Task.FromResult(i * 2)).ProcessInParallel(2);
        var results = await processor.GetResultsAsync();

        await Assert.That(results.OrderBy(x => x).SequenceEqual(Enumerable.Range(0, 10).Select(i => i * 2))).IsTrue();
    }

    [Test]
    public async Task Result_Processor_Enumerates_Input_Exactly_Once()
    {
        var source = new TrackingEnumerable(10);

        await using (var processor = source.SelectAsync(i => Task.FromResult(i)).ProcessInParallel())
        {
            _ = await processor.GetResultsAsync();
            _ = processor.GetEnumerableTasks().ToList();
            processor.CancelAll();
        }

        await Assert.That(source.EnumerationCount).IsEqualTo(1);
    }

    [Test]
    public async Task Iterator_Exception_Surfaces_At_Build_Time_Instead_Of_Hanging_Awaiters()
    {
        InvalidOperationException? caught = null;

        try
        {
            _ = FaultyItems().ForEachAsync(_ => Task.CompletedTask).ProcessInParallel();
        }
        catch (InvalidOperationException exception)
        {
            caught = exception;
        }

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.Message).IsEqualTo("enumeration failed");
    }

    private static IEnumerable<int> FaultyItems()
    {
        yield return 1;
        yield return 2;
        throw new InvalidOperationException("enumeration failed");
    }

    private sealed class TrackingEnumerable : IEnumerable<int>
    {
        private readonly int _count;

        public int EnumerationCount { get; private set; }
        public int ItemsYielded { get; private set; }

        public TrackingEnumerable(int count)
        {
            _count = count;
        }

        public IEnumerator<int> GetEnumerator()
        {
            EnumerationCount++;

            for (var i = 0; i < _count; i++)
            {
                ItemsYielded++;
                yield return i;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class OneShotEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _items;
        private bool _enumerated;

        public OneShotEnumerable(IEnumerable<T> items)
        {
            _items = items;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_enumerated)
            {
                throw new InvalidOperationException("This enumerable can only be enumerated once.");
            }

            _enumerated = true;
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
