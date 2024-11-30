using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.UnitTests.Extensions;

namespace EnumerableAsyncProcessor.UnitTests;

public class OneAtATimeAsyncProcessorTests
{
    [Test, Repeat(5), Timeout(10000)]
    public async Task When_Batch_Still_Processing_Then_Do_Not_Start_Next_Batch(CancellationToken cancellationToken)
    {
        var taskCount = 50;

        var taskCompletionSources = Enumerable.Range(0, taskCount).Select(_ => new TaskCompletionSource()).ToArray();
        var innerTasks = taskCompletionSources.Select(x => x.Task);

        var started = 0;

        var processor = innerTasks
            .ToAsyncProcessorBuilder()
            .ForEachAsync(async t =>
            {
                Interlocked.Increment(ref started);
                await t;
            })
            .ProcessOneAtATime();

        // Delay to make sure no other Tasks start
        await Task.Delay(100).ConfigureAwait(false);
        
        await Assert.That(started).IsEqualTo(1);
        
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion)).IsEqualTo(0);
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation)).IsEqualTo(50);
    }
    
    [Test, Repeat(5), Timeout(10000)]
    public async Task When_One_Finished_Then_One_More_Starts(CancellationToken cancellationToken)
    {
        var taskCount = 50;

        var taskCompletionSources = Enumerable.Range(0, taskCount).Select(_ => new TaskCompletionSource()).ToArray();
        var innerTasks = taskCompletionSources.Select(x => x.Task);

        var started = 0;

        var processor = innerTasks
            .ToAsyncProcessorBuilder()
            .ForEachAsync(async t =>
            {
                Interlocked.Increment(ref started);
                await t;
            })
            .ProcessOneAtATime();

        taskCompletionSources.First().SetResult();
        
        await processor.GetEnumerableTasks().First();
        
        // Delay to allow remaining Tasks to start
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        
        await Assert.That(started).IsEqualTo(2);
        
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion)).IsEqualTo(1);
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation)).IsEqualTo(49);
    }
    
    [Repeat(5), Timeout(10000)]
    [Test]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(5)]
    [Arguments(10)]
    public async Task When_Batch_Finished_Then_Start_Next_Batch(int amountToComplete, CancellationToken cancellationToken)
    {
        var taskCount = 50;

        var taskCompletionSources = Enumerable.Range(0, taskCount).Select(_ => new TaskCompletionSource()).ToArray();
        var innerTasks = taskCompletionSources.Select(x => x.Task);

        var started = 0;

        var processor = innerTasks
            .ToAsyncProcessorBuilder()
            .ForEachAsync(async t =>
            {
                Interlocked.Increment(ref started);
                await t;
            })
            .ProcessOneAtATime();

        Enumerable.Range(0, amountToComplete).ForEach(i => taskCompletionSources[i].SetResult());
        
        await Task.WhenAll(processor.GetEnumerableTasks().Take(amountToComplete));
        
        // Delay to allow remaining Tasks to start
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        
        await Assert.That(started).IsEqualTo(amountToComplete + 1);
        
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion)).IsEqualTo(amountToComplete);
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation)).IsEqualTo(50 - amountToComplete);
    }
}