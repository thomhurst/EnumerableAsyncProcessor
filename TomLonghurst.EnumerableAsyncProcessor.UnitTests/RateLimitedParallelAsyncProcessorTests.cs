using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using TomLonghurst.EnumerableAsyncProcessor.Builders;
using TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;
using TomLonghurst.EnumerableAsyncProcessor.UnitTests.Extensions;

namespace TomLonghurst.EnumerableAsyncProcessor.UnitTests;

[Parallelizable(ParallelScope.All)]
public class RateLimitedParallelAsyncProcessorTests
{
    [Test, Combinatorial, Retry(5)]
    public async Task Obey_Parallel_Limit(
        [Values(1, 2, 3, 5, 10, 15, 50, 100)] int parallelLimit, 
        [Values(1, 2, 3, 5, 10, 15, 50, 100)] int taskCount)
    {
        var taskCompletionSource = new TaskCompletionSource<string>();
        var blockingTask = taskCompletionSource.Task;
        var innerTasks = Enumerable.Range(0, taskCount).Select(i => new Task<Task<string>>(() => blockingTask, TaskCreationOptions.LongRunning)).ToArray();

        var started = 0;

        var processor = AsyncProcessorBuilder<Task<Task<string>>>.WithItems(innerTasks)
            .SelectAsync(async t =>
            {
                started++;
                t.Start();
                return await await t;
            })
            .ProcessInParallel(parallelLimit);
        
        await Task.WhenAll(innerTasks.Take(parallelLimit));

        var expectedStartedTasks = Math.Min(parallelLimit, taskCount);

        Assert.That(started, Is.EqualTo(expectedStartedTasks));
        
        Assert.That(innerTasks.Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(expectedStartedTasks));
        Assert.That(innerTasks.Count(x => x.Status == TaskStatus.Created), Is.EqualTo(Math.Max(taskCount - expectedStartedTasks, 0)));
        
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(0));
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation), Is.EqualTo(taskCount));

        taskCompletionSource.SetResult("Blah");
        
        await processor.GetResults();

        Assert.That(started, Is.EqualTo(taskCount));
        Assert.That(innerTasks.Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(taskCount));
        
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(taskCount));
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation), Is.EqualTo(0));
    }

    [Test, Retry(5)]
    public async Task When_Still_Tasks_Remaining_Then_Parallel_Limit_Still_Obeyed()
    {
        var taskCount = 50;
        var parallelLimit = 5;

        var taskCompletionSources = Enumerable.Range(0, taskCount).Select(i => new TaskCompletionSource<string>()).ToArray();
        var innerTasks = Enumerable.Range(0, taskCount).Select(i => new Task<Task<string>>(() => taskCompletionSources[i].Task, TaskCreationOptions.LongRunning)).ToArray();

        var started = 0;

        var processor = AsyncProcessorBuilder<Task<Task<string>>>.WithItems(innerTasks)
            .SelectAsync(async t =>
            {
                started++;
                t.Start();
                return await await t;
            })
            .ProcessInParallel(parallelLimit) as RateLimitedParallelAsyncProcessor<string>;

        Enumerable.Range(0, 40).ForEach(i => taskCompletionSources[i].SetResult("Blah"));
        
        await Task.WhenAll(innerTasks.Take(45));
        
        Assert.That(started, Is.EqualTo(45));
        
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(40));
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation), Is.EqualTo(10));
    }
    
    [Test, Retry(5)]
    public async Task When_Less_Tasks_Remaining_Than_Parallel_Limit_Then_Tasks_Remaining_Is_As_Expected()
    {
        var taskCount = 50;
        var parallelLimit = 5;

        var taskCompletionSources = Enumerable.Range(0, taskCount).Select(i => new TaskCompletionSource<string>()).ToArray();
        var innerTasks = Enumerable.Range(0, taskCount).Select(i => new Task<Task<string>>(() => taskCompletionSources[i].Task, TaskCreationOptions.LongRunning)).ToArray();

        var started = 0;

        var processor = AsyncProcessorBuilder<Task<Task<string>>>.WithItems(innerTasks)
            .SelectAsync(async t =>
            {
                started++;
                t.Start();
                return await await t;
            })
            .ProcessInParallel(parallelLimit) as RateLimitedParallelAsyncProcessor<string>;
        
        Enumerable.Range(0, 47).ForEach(i => taskCompletionSources[i].SetResult("Blah"));
        
        await Task.WhenAll(innerTasks);
        
        Assert.That(started, Is.EqualTo(50));
        
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(47));
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation), Is.EqualTo(3));
    }
}