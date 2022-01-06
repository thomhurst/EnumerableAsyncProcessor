using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using TomLonghurst.EnumerableAsyncProcessor.Extensions;
using TomLonghurst.EnumerableAsyncProcessor.UnitTests.Extensions;

namespace TomLonghurst.EnumerableAsyncProcessor.UnitTests;

public class BatchAsyncProcessorTests
{
    [Test, Retry(5), Timeout(10000)]
    public async Task When_Batch_Still_Processing_Then_Do_Not_Start_Next_Batch()
    {
        var taskCount = 50;
        var batchCount = 5;

        var taskCompletionSources = Enumerable.Range(0, taskCount).Select(_ => new TaskCompletionSource()).ToArray();
        var innerTasks = taskCompletionSources.Select(x => x.Task);

        var started = 0;

        var processor = innerTasks
            .ToAsyncProcessorBuilder()
            .ForEachAsync(async t =>
            {
                started++;
                await t;
            })
            .ProcessInBatches(batchCount);

        Enumerable.Range(0, 4).ForEach(i => taskCompletionSources[i].SetResult());
        
        await Task.WhenAll(processor.GetEnumerableTasks().Take(4));
        
        // Delay to make sure no other Tasks start
        await Task.Delay(100).ConfigureAwait(false);
        
        Assert.That(started, Is.EqualTo(5));
        
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(4));
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation), Is.EqualTo(46));
    }
    
    [Test, Retry(5), Timeout(10000)]
    public async Task When_Batch_Finished_Then_Start_Next_Batch()
    {
        var taskCount = 50;
        var batchCount = 5;

        var taskCompletionSources = Enumerable.Range(0, taskCount).Select(_ => new TaskCompletionSource()).ToArray();
        var innerTasks = taskCompletionSources.Select(x => x.Task);

        var started = 0;

        var processor = innerTasks
            .ToAsyncProcessorBuilder()
            .ForEachAsync(async t =>
            {
                started++;
                await t;
            })
            .ProcessInBatches(batchCount);

        Enumerable.Range(0, 5).ForEach(i => taskCompletionSources[i].SetResult());
        
        await Task.WhenAll(processor.GetEnumerableTasks().Take(4));
        
        // Delay to allow remaining Tasks to start
        await Task.Delay(100).ConfigureAwait(false);
        
        Assert.That(started, Is.EqualTo(10));
        
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(5));
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation), Is.EqualTo(45));
    }
}