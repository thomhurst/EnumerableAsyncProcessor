using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.UnitTests;

public class BatchAsyncProcessorTests
{
    [Test, Repeat(5)]
    public async Task When_Batch_Still_Processing_Then_Do_Not_Start_Next_Batch(CancellationToken cancellationToken)
    {
        const int taskCount = 50;
        const int batchCount = 5;

        var taskCompletionSources = Enumerable.Range(0, taskCount).Select(_ => new TaskCompletionSource()).ToArray();
        var innerTasks = taskCompletionSources.Select(x => x.Task);

        var started = 0;

        var processor = innerTasks
            .ToAsyncProcessorBuilder()
            .ForEachAsync(async t =>
            {
                Interlocked.Increment(ref started);
                await t;
            }, cancellationToken)
            .ProcessInBatches(batchCount);

        foreach (var taskCompletionSource in taskCompletionSources.Take(4))
        {
            taskCompletionSource.SetResult();
        }
        
        await Task.WhenAll(processor.GetEnumerableTasks().Take(4));
        
        // Delay to make sure no other Tasks start
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        
        await Assert.That(started).IsEqualTo(5);
        
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion)).IsEqualTo(4);
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation)).IsEqualTo(46);
    }
    
    [Test, Repeat(5)]
    public async Task When_Batch_Finished_Then_Start_Next_Batch(CancellationToken cancellationToken)
    {
        const int taskCount = 50;
        const int batchCount = 5;

        var taskCompletionSources = Enumerable.Range(0, taskCount).Select(_ => new TaskCompletionSource()).ToArray();
        var innerTasks = taskCompletionSources.Select(x => x.Task);

        var started = 0;

        var processor = innerTasks
            .ToAsyncProcessorBuilder()
            .ForEachAsync(async t =>
            {
                Interlocked.Increment(ref started);
                await t;
            }, cancellationToken)
            .ProcessInBatches(batchCount);

        foreach (var taskCompletionSource in taskCompletionSources.Take(5))
        {
            taskCompletionSource.SetResult();
        }

        var enumerableTasks = processor.GetEnumerableTasks().ToArray();
        
        await Task.WhenAll(enumerableTasks.Take(5));
        
        // Delay to allow remaining Tasks to start
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        
        await Assert.That(started).IsEqualTo(10);
        
        await Assert.That(enumerableTasks.Count(x => x.Status == TaskStatus.RanToCompletion)).IsEqualTo(5);
        await Assert.That(enumerableTasks.Count(x => x.Status == TaskStatus.WaitingForActivation)).IsEqualTo(45);
    }
}