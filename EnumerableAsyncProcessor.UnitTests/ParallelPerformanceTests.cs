using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumerableAsyncProcessor.Builders;

namespace EnumerableAsyncProcessor.UnitTests;

public class ParallelPerformanceTests
{
    [Test]
    public async Task MeasureParallelProcessingTime_10000ItemsWith1SecondDelay()
    {
        const int itemCount = 10000;
        const int delaySeconds = 1;
        
        var stopwatch = Stopwatch.StartNew();
        
        var processor = AsyncProcessorBuilder
            .WithItems(Enumerable.Range(0, itemCount))
            .ForEachAsync(_ => Task.Delay(TimeSpan.FromSeconds(delaySeconds)))
            .ProcessInParallel();

        await processor;
        
        stopwatch.Stop();
        
        var completedTasks = processor.GetEnumerableTasks().Count(x => x.IsCompleted);
        
        await Assert.That(completedTasks).IsEqualTo(itemCount);
        
        Console.WriteLine($"Total execution time: {stopwatch.Elapsed}");
        Console.WriteLine($"Total execution time (seconds): {stopwatch.Elapsed.TotalSeconds:F2}");
        Console.WriteLine($"Total execution time (milliseconds): {stopwatch.Elapsed.TotalMilliseconds:F0}");
        
        var expectedSequentialTime = itemCount * delaySeconds;
        Console.WriteLine($"Expected sequential time: {expectedSequentialTime:N0} seconds");
        Console.WriteLine($"Speedup factor: {expectedSequentialTime / stopwatch.Elapsed.TotalSeconds:F2}x");
        
        await Assert.That(stopwatch.Elapsed.TotalSeconds).IsLessThan(10);
    }
    
    [Test]
    public async Task MeasureParallelProcessingTime_1000ItemsWith100msDelay()
    {
        const int itemCount = 1000;
        const int delayMilliseconds = 100;
        
        var stopwatch = Stopwatch.StartNew();
        
        var processor = AsyncProcessorBuilder
            .WithItems(Enumerable.Range(0, itemCount))
            .ForEachAsync(_ => Task.Delay(delayMilliseconds))
            .ProcessInParallel();

        await processor;
        
        stopwatch.Stop();
        
        var completedTasks = processor.GetEnumerableTasks().Count(x => x.IsCompleted);
        
        await Assert.That(completedTasks).IsEqualTo(itemCount);
        
        Console.WriteLine($"Total execution time: {stopwatch.Elapsed}");
        Console.WriteLine($"Total execution time (seconds): {stopwatch.Elapsed.TotalSeconds:F2}");
        Console.WriteLine($"Total execution time (milliseconds): {stopwatch.Elapsed.TotalMilliseconds:F0}");
        
        var expectedSequentialTimeMs = itemCount * delayMilliseconds;
        Console.WriteLine($"Expected sequential time: {expectedSequentialTimeMs:N0} milliseconds ({expectedSequentialTimeMs/1000.0:F1} seconds)");
        Console.WriteLine($"Speedup factor: {expectedSequentialTimeMs / stopwatch.Elapsed.TotalMilliseconds:F2}x");
        
        await Assert.That(stopwatch.Elapsed.TotalSeconds).IsLessThan(5);
    }
    
    [Test]
    public async Task MeasureParallelProcessingTime_1000ItemsWithThreadSleep()
    {
        const int itemCount = 1000;
        const int sleepMilliseconds = 100;
        
        var stopwatch = Stopwatch.StartNew();
        
        var processor = AsyncProcessorBuilder
            .WithItems(Enumerable.Range(0, itemCount))
            .ForEachAsync(_ => Task.Run(() => Thread.Sleep(sleepMilliseconds)))
            .ProcessInParallel();

        await processor;
        
        stopwatch.Stop();
        
        var completedTasks = processor.GetEnumerableTasks().Count(x => x.IsCompleted);
        
        await Assert.That(completedTasks).IsEqualTo(itemCount);
        
        Console.WriteLine($"Total execution time with Thread.Sleep: {stopwatch.Elapsed}");
        Console.WriteLine($"Total execution time (seconds): {stopwatch.Elapsed.TotalSeconds:F2}");
        Console.WriteLine($"Total execution time (milliseconds): {stopwatch.Elapsed.TotalMilliseconds:F0}");
        
        var expectedSequentialTimeMs = itemCount * sleepMilliseconds;
        Console.WriteLine($"Expected sequential time: {expectedSequentialTimeMs:N0} milliseconds ({expectedSequentialTimeMs/1000.0:F1} seconds)");
        Console.WriteLine($"Speedup factor: {expectedSequentialTimeMs / stopwatch.Elapsed.TotalMilliseconds:F2}x");
        
        await Assert.That(stopwatch.Elapsed.TotalSeconds).IsLessThan(10);
    }
    
    [Test]
    public async Task MeasureParallelProcessingTime_200ItemsWithDirectThreadSleep()
    {
        const int itemCount = 1000;
        const int sleepMilliseconds = 100;
        
        var stopwatch = Stopwatch.StartNew();
        
        var processor = AsyncProcessorBuilder
            .WithItems(Enumerable.Range(0, itemCount))
            .ForEachAsync(_ => 
            {
                Thread.Sleep(sleepMilliseconds);
                return Task.CompletedTask;
            })
            .ProcessInParallel();

        await processor;
        
        stopwatch.Stop();
        
        var completedTasks = processor.GetEnumerableTasks().Count(x => x.IsCompleted);
        
        await Assert.That(completedTasks).IsEqualTo(itemCount);
        
        Console.WriteLine($"Total execution time with direct Thread.Sleep: {stopwatch.Elapsed}");
        Console.WriteLine($"Total execution time (seconds): {stopwatch.Elapsed.TotalSeconds:F2}");
        Console.WriteLine($"Total execution time (milliseconds): {stopwatch.Elapsed.TotalMilliseconds:F0}");
        
        var expectedSequentialTimeMs = itemCount * sleepMilliseconds;
        Console.WriteLine($"Expected sequential time: {expectedSequentialTimeMs:N0} milliseconds ({expectedSequentialTimeMs/1000.0:F1} seconds)");
        Console.WriteLine($"Speedup factor: {expectedSequentialTimeMs / stopwatch.Elapsed.TotalMilliseconds:F2}x");
        
        await Assert.That(stopwatch.Elapsed.TotalSeconds).IsLessThan(15);
    }
}