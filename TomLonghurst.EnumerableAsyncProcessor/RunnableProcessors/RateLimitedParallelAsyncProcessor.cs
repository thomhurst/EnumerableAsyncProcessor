namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class RateLimitedParallelAsyncProcessor<TResult> : AbstractAsyncProcessor<TResult>
{
    private readonly int _levelsOfParallelism;
    private Task _totalProgressTask;

    public RateLimitedParallelAsyncProcessor(List<Task<Task<TResult>>> initialTasks, int levelsOfParallelism,
        CancellationTokenSource cancellationTokenSource) : base(initialTasks, cancellationTokenSource)
    {
        _levelsOfParallelism = levelsOfParallelism;
    }

    internal override Task Process()
    {
        _totalProgressTask = Task.WhenAll(UnwrappedTasks);
        
        return Parallel.ForEachAsync(InitialTasks,
            new ParallelOptions { MaxDegreeOfParallelism = _levelsOfParallelism, CancellationToken = CancellationToken},
            async (task, token) =>
            {
                task.Start();
                await await task;
            });
    }

    public override Task ContinuationTask => _totalProgressTask;
}

public class RateLimitedParallelAsyncProcessor : AbstractAsyncProcessor
{
    private readonly int _levelsOfParallelism;
    private Task _totalProgressTask;

    public RateLimitedParallelAsyncProcessor(List<Task<Task>> initialTasks, int levelsOfParallelism,
        CancellationTokenSource cancellationTokenSource) : base(initialTasks, cancellationTokenSource)
    {
        _levelsOfParallelism = levelsOfParallelism;
    }

    internal override Task Process()
    {
        _totalProgressTask = Task.WhenAll(UnwrappedTasks);
        
        return Parallel.ForEachAsync(InitialTasks,
            new ParallelOptions { MaxDegreeOfParallelism = _levelsOfParallelism, CancellationToken = CancellationToken},
            async (task, token) =>
            {
                task.Start();
                await await task;
            });
    }

    public override Task Task => _totalProgressTask;
}