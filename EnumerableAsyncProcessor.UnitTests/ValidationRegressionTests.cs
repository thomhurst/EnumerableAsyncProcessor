using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Builders;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.UnitTests;

/// <summary>
/// Guards argument validation:
/// - invalid parallelism/batch/timespan values must throw at build time on every processor variant
///   (the result variants previously skipped validation, and maxConcurrency: 0 previously surfaced
///   as a semaphore error mid-processing),
/// - the old arbitrary caps (10,000 tasks / 10,000 batch size) must stay removed,
/// - an already-cancelled token must fail cleanly at build time.
/// </summary>
public class ValidationRegressionTests
{
    [Test]
    public async Task Zero_Or_Negative_MaxConcurrency_Throws_At_Build_Time()
    {
        await AssertThrows<ArgumentOutOfRangeException>(() =>
            new[] { 1 }.ForEachAsync(_ => Task.CompletedTask).ProcessInParallel(maxConcurrency: 0));

        await AssertThrows<ArgumentOutOfRangeException>(() =>
            new[] { 1 }.ForEachAsync(_ => Task.CompletedTask).ProcessInParallel(maxConcurrency: -1));

        await AssertThrows<ArgumentOutOfRangeException>(() =>
            new[] { 1 }.SelectAsync(i => Task.FromResult(i)).ProcessInParallel(maxConcurrency: 0));
    }

    [Test]
    public async Task Zero_MaxConcurrency_Throws_For_Positional_Void_And_Result_Calls()
    {
        await AssertThrows<ArgumentOutOfRangeException>(() =>
            new[] { 1 }.ForEachAsync(_ => Task.CompletedTask).ProcessInParallel(0));

        // The result variant previously skipped this validation entirely
        await AssertThrows<ArgumentOutOfRangeException>(() =>
            new[] { 1 }.SelectAsync(i => Task.FromResult(i)).ProcessInParallel(0));
    }

    [Test]
    public async Task Invalid_Timed_RateLimit_Arguments_Throw_For_Void_And_Result_Processors()
    {
        await AssertThrows<ArgumentOutOfRangeException>(() =>
            new[] { 1 }.ForEachAsync(_ => Task.CompletedTask).ProcessInParallel(5, TimeSpan.FromSeconds(-1)));

        // The result variant previously skipped this validation entirely
        await AssertThrows<ArgumentOutOfRangeException>(() =>
            new[] { 1 }.SelectAsync(i => Task.FromResult(i)).ProcessInParallel(0, TimeSpan.FromSeconds(1)));
    }

    [Test]
    public async Task Zero_Batch_Size_Throws_For_Void_And_Result_Processors()
    {
        await AssertThrows<ArgumentOutOfRangeException>(() =>
            new[] { 1 }.ForEachAsync(_ => Task.CompletedTask).ProcessInBatches(0));

        // The result variant previously skipped this validation entirely
        await AssertThrows<ArgumentOutOfRangeException>(() =>
            new[] { 1 }.SelectAsync(i => Task.FromResult(i)).ProcessInBatches(0));
    }

    [Test]
    public async Task Large_Execution_Counts_Are_Not_Capped()
    {
        // Counts above 10,000 previously threw to "prevent system overload"
        var executed = 0;

        await using var processor = AsyncProcessorBuilder.WithExecutionCount(15_000)
            .ForEachAsync(() =>
            {
                Interlocked.Increment(ref executed);
                return Task.CompletedTask;
            })
            .ProcessInParallel(64);

        await processor.WaitAsync();

        await Assert.That(executed).IsEqualTo(15_000);
    }

    [Test]
    public async Task Large_Batch_Sizes_Are_Not_Capped()
    {
        // Batch sizes above 10,000 previously threw
        await using var processor = Enumerable.Range(0, 100).ToList()
            .ForEachAsync(_ => Task.CompletedTask)
            .ProcessInBatches(12_000);

        await processor.WaitAsync();

        await Assert.That(processor.GetEnumerableTasks().Count(x => x.IsCompletedSuccessfully)).IsEqualTo(100);
    }

    [Test]
    public async Task Already_Cancelled_Token_Fails_Cleanly_At_Build_Time()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Must throw a clear ArgumentException - never a NullReferenceException from a
        // cancellation callback firing on a partially constructed processor
        await AssertThrows<ArgumentException>(() =>
            new[] { 1 }.ForEachAsync(_ => Task.CompletedTask, cancellationTokenSource.Token).ProcessInParallel());
    }

    private static async Task AssertThrows<TException>(Func<object> action) where TException : Exception
    {
        TException? caught = null;

        try
        {
            _ = action();
        }
        catch (TException exception)
        {
            caught = exception;
        }

        await Assert.That(caught).IsNotNull();
    }
}
