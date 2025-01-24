using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Builders;
using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.UnitTests.Extensions;

namespace EnumerableAsyncProcessor.UnitTests;

public class RateLimitedParallelAsyncProcessorTests
{
    [MatrixDataSource]
    [Test, Repeat(5), Timeout(10000)]
    public async Task Obey_Parallel_Limit(
        [Matrix(1, 2, 3, 5, 10, 15, 50, 100)] int parallelLimit, 
        [Matrix(1, 2, 3, 5, 10, 15, 50, 100)] int taskCount,
        CancellationToken cancellationToken)
    {
        var taskCompletionSource = new TaskCompletionSource<string>();
        var blockingTask = taskCompletionSource.Task;
        var innerTasks = Enumerable.Range(0, taskCount).Select(_ => new Task<Task>(() => blockingTask, TaskCreationOptions.LongRunning)).ToArray();

        var started = 0;

        var processor = AsyncProcessorBuilder.WithItems(innerTasks)
            .ForEachAsync(async t =>
            {
                Interlocked.Increment(ref started);
                t.Start();
                await await t;
            })
            .ProcessInParallel(parallelLimit);
        
        await Task.WhenAll(innerTasks.Take(parallelLimit));
        // Delay to allow remaining Tasks to start
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);

        var expectedStartedTasks = Math.Min(parallelLimit, taskCount);

        await Assert.That(started).IsEqualTo(expectedStartedTasks);
        
        await Assert.That(innerTasks.Count(x => x.Status == TaskStatus.RanToCompletion)).IsEqualTo(expectedStartedTasks);
        await Assert.That(innerTasks.Count(x => x.Status == TaskStatus.Created)).IsEqualTo(Math.Max(taskCount - expectedStartedTasks, 0));
        
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion)).IsEqualTo(0);
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation)).IsEqualTo(taskCount);

        taskCompletionSource.SetResult("Blah");
        
        await processor;

        await Assert.That(started).IsEqualTo(taskCount);
        await Assert.That(innerTasks.Count(x => x.Status == TaskStatus.RanToCompletion)).IsEqualTo(taskCount);
        
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion)).IsEqualTo(taskCount);
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation)).IsEqualTo(0);
    }

    [Test, Repeat(5), Timeout(10000)]
    public async Task When_Still_Tasks_Remaining_Then_Parallel_Limit_Still_Obeyed(CancellationToken cancellationToken)
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
                Interlocked.Increment(ref started);
                await t;
            })
            .ProcessInParallel(parallelLimit);

        Enumerable.Range(0, 40).ForEach(i => taskCompletionSources[i].SetResult());
        
        await Task.WhenAll(processor.GetEnumerableTasks().Take(40));
        // Delay to allow remaining Tasks to start
        await Task.Delay(100).ConfigureAwait(false);
        
        await Assert.That(started).IsEqualTo(45);
        
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion)).IsEqualTo(40);
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation)).IsEqualTo(10);
    }
    
    [Test, Repeat(5), Timeout(10000)]
    public async Task When_Still_Tasks_Remaining_And_Cancel_Then_Cancel_Unstarted_Tasks_And_Finish_Currently_Running(CancellationToken cancellationToken)
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
        
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion)).IsEqualTo(40);
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation)).IsEqualTo(10);

        // Delay to allow remaining Tasks to start
        await Task.Delay(100).ConfigureAwait(false);
        
        cancellationTokenSource.Cancel();
        await Assert.ThrowsAsync<TaskCanceledException>(() => processor.WaitAsync());

        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion)).IsEqualTo(40);
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.Canceled)).IsEqualTo(10);
    }
    
    [Test, Repeat(5), Timeout(10000)]
    public async Task When_Less_Tasks_Remaining_Than_Parallel_Limit_Then_Tasks_Remaining_Is_As_Expected(CancellationToken cancellationToken)
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
                Interlocked.Increment(ref started);
                await t;
            })
            .ProcessInParallel(parallelLimit);
        
        Enumerable.Range(0, 47).ForEach(i => taskCompletionSources[i].SetResult());
        
        await Task.WhenAll(processor.GetEnumerableTasks().Take(47));
        // Delay to allow remaining Tasks to start
        await Task.Delay(100).ConfigureAwait(false);
        
        await Assert.That(started).IsEqualTo(50);
        
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.RanToCompletion)).IsEqualTo(47);
        await Assert.That(processor.GetEnumerableTasks().Count(x => x.Status == TaskStatus.WaitingForActivation)).IsEqualTo(3);
    }
}