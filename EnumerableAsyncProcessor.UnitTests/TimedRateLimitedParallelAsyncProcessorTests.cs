using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using EnumerableAsyncProcessor.Builders;

namespace EnumerableAsyncProcessor.UnitTests;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
public class TimedRateLimitedParallelAsyncProcessorTests
{
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public async Task Test(int secondsToRateLimit)
    {
        var processor = AsyncProcessorBuilder
            .WithExecutionCount(500)
            .ForEachAsync( () => Task.Delay(100))
            .ProcessInParallel(100, TimeSpan.FromSeconds(secondsToRateLimit));

        await Task.Delay(TimeSpan.FromSeconds(secondsToRateLimit));

        var completedTasks = processor.GetEnumerableTasks().Count(x => x.IsCompleted);
        
        await TestContext.Out.WriteLineAsync($"Complete Count is {completedTasks}");
        
        Assert.That(completedTasks, Is.EqualTo(100));
        
        await Task.Delay(TimeSpan.FromSeconds(secondsToRateLimit));
        
        completedTasks = processor.GetEnumerableTasks().Count(x => x.IsCompleted);

        await TestContext.Out.WriteLineAsync($"Complete Count is {completedTasks}");
        
        Assert.That(completedTasks, Is.EqualTo(200));
    }
}