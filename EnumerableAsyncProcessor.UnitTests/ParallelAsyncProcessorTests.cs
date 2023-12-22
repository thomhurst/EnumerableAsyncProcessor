using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Builders;
using NUnit.Framework;

namespace EnumerableAsyncProcessor.UnitTests;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
public class ParallelAsyncProcessorTests
{
    [Test]
    public async Task Test()
    {
        var stopwatch = Stopwatch.StartNew();
        
        var processor = AsyncProcessorBuilder
            .WithExecutionCount(500)
            .ForEachAsync( () => Task.Delay(100))
            .ProcessInParallel();

        await processor;
        
        stopwatch.Stop();

        var completedTasks = processor.GetEnumerableTasks().Count(x => x.IsCompleted);
        
        Assert.That(completedTasks, Is.EqualTo(500));
        Assert.That(stopwatch.Elapsed.Milliseconds, Is.EqualTo(100).Within(1000));
    }
}