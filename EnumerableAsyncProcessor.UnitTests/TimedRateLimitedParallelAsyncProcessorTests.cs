using System;
using System.Linq;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Builders;

namespace EnumerableAsyncProcessor.UnitTests;

public class TimedRateLimitedParallelAsyncProcessorTests
{
    [Test]
    [Arguments(2)]
    [Arguments(4)]
    [Arguments(6)]
    [Arguments(8)]
    [Arguments(10)]
    public async Task Test(int secondsToRateLimit)
    {
        var processor = AsyncProcessorBuilder
            .WithExecutionCount(500)
            .ForEachAsync(() => Task.Delay(100))
            .ProcessInParallel(100, TimeSpan.FromSeconds(secondsToRateLimit));

        await Task.Delay(TimeSpan.FromSeconds(secondsToRateLimit));

        var completedTasks = processor.GetEnumerableTasks().Count(x => x.IsCompleted);
        
        Console.WriteLine($"Complete Count is {completedTasks}");
        
        await Assert.That(completedTasks).IsEqualTo(100);
        
        await Task.Delay(TimeSpan.FromSeconds(secondsToRateLimit));
        
        completedTasks = processor.GetEnumerableTasks().Count(x => x.IsCompleted);

        Console.WriteLine($"Complete Count is {completedTasks}");
        
        await Assert.That(completedTasks).IsEqualTo(200);
    }
}