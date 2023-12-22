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
        var processor = AsyncProcessorBuilder
            .WithExecutionCount(500)
            .ForEachAsync( () => Task.Delay(100))
            .ProcessInParallel();

        await processor;
        
        var completedTasks = processor.GetEnumerableTasks().Count(x => x.IsCompleted);
        
        Assert.That(completedTasks, Is.EqualTo(500));
    }
    
    [Test]
    public async Task Test2()
    {
        var processor = AsyncProcessorBuilder
            .WithItems(Enumerable.Range(0, 500))
            .ForEachAsync(i => Task.Delay(100))
            .ProcessInParallel();

        await processor;
        
        var completedTasks = processor.GetEnumerableTasks().Count(x => x.IsCompleted);
        
        Assert.That(completedTasks, Is.EqualTo(500));
    }
    
    [Test]
    public async Task Test3()
    {
        var processor = AsyncProcessorBuilder
            .WithItems(Enumerable.Range(0, 500))
            .SelectAsync(Task.FromResult)
            .ProcessInParallel();

        await processor;
        
        var completedTasks = processor.GetEnumerableTasks().Count(x => x.IsCompleted);
        
        Assert.That(completedTasks, Is.EqualTo(500));
    }
    
    [Test]
    public async Task Test4()
    {
        var random = new Random();
        
        var processor = AsyncProcessorBuilder
            .WithExecutionCount(500)
            .SelectAsync( () => Task.FromResult(random.Next()))
            .ProcessInParallel();

        await processor;
        
        var completedTasks = processor.GetEnumerableTasks().Count(x => x.IsCompleted);
        
        Assert.That(completedTasks, Is.EqualTo(500));
    }
}