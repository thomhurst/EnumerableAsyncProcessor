using BenchmarkDotNet.Attributes;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.Benchmarks;

[MemoryDiagnoser]
public class ProcessorBenchmarks
{
    private const int Concurrency = 64;
    private int[] _items = null!;
    private Func<int, Task> _processItemAsync = null!;
    private Func<int, Task<int>> _transformItemAsync = null!;

    public enum SelectorWorkload
    {
        CompletedTask,
        TaskYield
    }

    [Params(1_000, 10_000, 100_000)]
    public int ItemCount { get; set; }

    [Params(SelectorWorkload.CompletedTask, SelectorWorkload.TaskYield)]
    public SelectorWorkload Workload { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _items = Enumerable.Range(0, ItemCount).ToArray();
        _processItemAsync = Workload == SelectorWorkload.CompletedTask
            ? ProcessCompletedItemAsync
            : ProcessWithYieldAsync;
        _transformItemAsync = Workload == SelectorWorkload.CompletedTask
            ? TransformCompletedItemAsync
            : TransformWithYieldAsync;
    }

    [Benchmark(Baseline = true)]
    public async Task UnboundedParallel()
    {
        await using var processor = _items
            .ForEachAsync(_processItemAsync)
            .ProcessInParallel();

        await processor.WaitAsync();
    }

    [Benchmark]
    public async Task ThrottledParallel()
    {
        await using var processor = _items
            .ForEachAsync(_processItemAsync)
            .ProcessInParallel(maxConcurrency: Concurrency);

        await processor.WaitAsync();
    }

    [Benchmark]
    public async Task RateLimitedParallel()
    {
        await using var processor = _items
            .ForEachAsync(_processItemAsync)
            .ProcessInParallel(Concurrency);

        await processor.WaitAsync();
    }

    [Benchmark]
    public async Task TimedRateLimitedParallel()
    {
        await using var processor = _items
            .ForEachAsync(_processItemAsync)
            .ProcessInParallel(Concurrency, TimeSpan.Zero);

        await processor.WaitAsync();
    }

    [Benchmark]
    public async Task Batch()
    {
        await using var processor = _items
            .ForEachAsync(_processItemAsync)
            .ProcessInBatches(Concurrency);

        await processor.WaitAsync();
    }

    [Benchmark]
    public async Task OneAtATime()
    {
        await using var processor = _items
            .ForEachAsync(_processItemAsync)
            .ProcessOneAtATime();

        await processor.WaitAsync();
    }

    [Benchmark]
    public async Task<int> ResultStreaming()
    {
        await using var processor = _items
            .SelectAsync(_transformItemAsync)
            .ProcessInParallel(maxConcurrency: Concurrency);

        var checksum = 0;
        await foreach (var result in processor.GetResultsAsyncEnumerable())
        {
            checksum = unchecked(checksum + result);
        }

        return checksum;
    }

    private static Task ProcessCompletedItemAsync(int _)
    {
        return Task.CompletedTask;
    }

    private static Task<int> TransformCompletedItemAsync(int item)
    {
        return Task.FromResult(item);
    }

    private static async Task ProcessWithYieldAsync(int _)
    {
        await Task.Yield();
    }

    private static async Task<int> TransformWithYieldAsync(int item)
    {
        await Task.Yield();
        return item;
    }
}
