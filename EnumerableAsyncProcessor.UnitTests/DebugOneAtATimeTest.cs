using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.UnitTests.Extensions;

namespace EnumerableAsyncProcessor.UnitTests;

public class DebugOneAtATimeTest
{
    [Test]
    public async Task Debug_When_One_Finished_Then_One_More_Starts()
    {
        var taskCount = 5;
        var taskCompletionSources = Enumerable.Range(0, taskCount).Select(_ => new TaskCompletionSource()).ToArray();
        var innerTasks = taskCompletionSources.Select(x => x.Task);

        var started = 0;
        var startTimes = new DateTime[taskCount];

        var processor = innerTasks
            .ToAsyncProcessorBuilder()
            .ForEachAsync(async t =>
            {
                var current = Interlocked.Increment(ref started) - 1;
                startTimes[current] = DateTime.Now;
                Console.WriteLine($"Task {current} started at {startTimes[current]:HH:mm:ss.fff}");
                await t;
                Console.WriteLine($"Task {current} completed at {DateTime.Now:HH:mm:ss.fff}");
            })
            .ProcessOneAtATime();

        Console.WriteLine($"Initial started count: {started}");
        
        // Wait a bit for first task to start
        await Task.Delay(500);
        Console.WriteLine($"After 500ms delay, started count: {started}");
        
        // Complete the first task
        Console.WriteLine("Setting result for first task...");
        taskCompletionSources[0].SetResult();
        
        // Wait for it to complete
        await processor.GetEnumerableTasks().First();
        Console.WriteLine($"First task completed. Started count: {started}");
        
        // Wait for second task to start with exponential backoff
        var maxWaitTime = 10000; // 10 seconds max
        var waitedTime = 0;
        var delay = 100;
        
        while (started < 2 && waitedTime < maxWaitTime)
        {
            await Task.Delay(delay);
            waitedTime += delay;
            delay = Math.Min(delay * 2, 1000); // Exponential backoff up to 1 second
            Console.WriteLine($"After {waitedTime}ms total wait, started count: {started}");
        }
        
        Console.WriteLine($"Final started count: {started}");
        
        // Assertions
        await Assert.That(started).IsEqualTo(2);
        
        // Clean up
        foreach (var tcs in taskCompletionSources.Skip(1))
        {
            tcs.SetResult();
        }
        await processor.WaitAsync();
    }
}