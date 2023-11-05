using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.UnitTests.Extensions;

namespace EnumerableAsyncProcessor.UnitTests;

public class OneAtATimeAsyncProcessorTests
{
    [Test, Repeat(5), Timeout(10000)]
    public async Task When_Batch_Still_Processing_Then_Do_Not_Start_Next_Batch()
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
        
        Assert.That(started, Is.EqualTo(1));
        
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(0));
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation), Is.EqualTo(50));
    }
    
        [Test, Repeat(5), Timeout(10000)]
    public async Task When_One_Finished_Then_One_More_Starts()
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
        await Task.Delay(100).ConfigureAwait(false);
        
        Assert.That(started, Is.EqualTo(2));
        
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(1));
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation), Is.EqualTo(49));
    }
    
    [Repeat(5), Timeout(10000)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(5)]
    [TestCase(10)]
    public async Task When_Batch_Finished_Then_Start_Next_Batch(int amountToComplete)
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
        await Task.Delay(100).ConfigureAwait(false);
        
        Assert.That(started, Is.EqualTo(amountToComplete + 1));
        
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(amountToComplete));
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation), Is.EqualTo(50 - amountToComplete));
    }
}