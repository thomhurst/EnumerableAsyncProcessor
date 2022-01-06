using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TomLonghurst.EnumerableAsyncProcessor.Builders;
using TomLonghurst.EnumerableAsyncProcessor.Extensions;
using TomLonghurst.EnumerableAsyncProcessor.UnitTests.Extensions;

namespace TomLonghurst.EnumerableAsyncProcessor.UnitTests;

[Parallelizable(ParallelScope.All)]
public class RateLimitedParallelAsyncProcessorTests
{
    [Test, Combinatorial, Retry(5), Timeout(10000)]
    public async Task Obey_Parallel_Limit(
        [Values(1, 2, 3, 5, 10, 15, 50, 100)] int parallelLimit, 
        [Values(1, 2, 3, 5, 10, 15, 50, 100)] int taskCount)
    {
        var taskCompletionSource = new TaskCompletionSource<string>();
        var blockingTask = taskCompletionSource.Task;
        var innerTasks = Enumerable.Range(0, taskCount).Select(_ => new Task<Task>(() => blockingTask, TaskCreationOptions.LongRunning)).ToArray();

        var started = 0;

        var processor = AsyncProcessorBuilder.WithItems(innerTasks)
            .ForEachAsync(async t =>
            {
                started++;
                t.Start();
                await await t;
            })
            .ProcessInParallel(parallelLimit);
        
        await Task.WhenAll(innerTasks.Take(parallelLimit));
        // Delay to allow remaining Tasks to start
        await Task.Delay(100).ConfigureAwait(false);

        var expectedStartedTasks = Math.Min(parallelLimit, taskCount);

        Assert.That(started, Is.EqualTo(expectedStartedTasks));
        
        Assert.That(innerTasks.Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(expectedStartedTasks));
        Assert.That(innerTasks.Count(x => x.Status == TaskStatus.Created), Is.EqualTo(Math.Max(taskCount - expectedStartedTasks, 0)));
        
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(0));
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation), Is.EqualTo(taskCount));

        taskCompletionSource.SetResult("Blah");
        
        await processor;

        Assert.That(started, Is.EqualTo(taskCount));
        Assert.That(innerTasks.Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(taskCount));
        
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(taskCount));
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation), Is.EqualTo(0));
    }

    [Test, Retry(5), Timeout(10000)]
    public async Task When_Still_Tasks_Remaining_Then_Parallel_Limit_Still_Obeyed()
    {
        var taskCount = 50;
        var parallelLimit = 5;

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
            .ProcessInParallel(parallelLimit);

        Enumerable.Range(0, 40).ForEach(i => taskCompletionSources[i].SetResult());
        
        await Task.WhenAll(processor.GetEnumerableTasks().Take(40));
        // Delay to allow remaining Tasks to start
        await Task.Delay(100).ConfigureAwait(false);
        
        Assert.That(started, Is.EqualTo(45));
        
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(40));
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation), Is.EqualTo(10));
    }
    
    [Test, Retry(5), Timeout(10000)]
    public async Task When_Still_Tasks_Remaining_And_Cancel_Then_Cancel_Unstarted_Tasks_And_Finish_Currently_Running()
    {
        var taskCount = 50;
        var parallelLimit = 5;
        var cancellationTokenSource = new CancellationTokenSource();

        var taskCompletionSources = Enumerable.Range(0, taskCount).Select(_ => new TaskCompletionSource()).ToArray();
        var innerTasks = taskCompletionSources.Select(x => x.Task);
        
        var processor = innerTasks
            .ToAsyncProcessorBuilder()
            .ForEachAsync(async t =>
            {
                await t;
            }, cancellationTokenSource.Token)
            .ProcessInParallel(parallelLimit);

        Enumerable.Range(0, 40).ForEach(i => taskCompletionSources[i].SetResult());
        await Task.WhenAll(processor.GetEnumerableTasks().Take(40));
        // Delay to allow remaining Tasks to start
        await Task.Delay(100).ConfigureAwait(false);
        
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(40));
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation), Is.EqualTo(10));

        // Delay to allow remaining Tasks to start
        await Task.Delay(100).ConfigureAwait(false);
        
        cancellationTokenSource.Cancel();
        taskCompletionSources.Skip(40).ForEach(taskCompletionSource => taskCompletionSource.SetResult());
        Assert.ThrowsAsync<TaskCanceledException>(() => processor.WaitAsync());

        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(40));
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.Canceled), Is.EqualTo(10));
    }
    
    [Test, Retry(5), Timeout(10000)]
    public async Task When_Less_Tasks_Remaining_Than_Parallel_Limit_Then_Tasks_Remaining_Is_As_Expected()
    {
        var taskCount = 50;
        var parallelLimit = 5;

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
            .ProcessInParallel(parallelLimit);
        
        Enumerable.Range(0, 47).ForEach(i => taskCompletionSources[i].SetResult());
        
        await Task.WhenAll(processor.GetEnumerableTasks().Take(47));
        // Delay to allow remaining Tasks to start
        await Task.Delay(100).ConfigureAwait(false);
        
        Assert.That(started, Is.EqualTo(50));
        
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion), Is.EqualTo(47));
        Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation), Is.EqualTo(3));
    }
}