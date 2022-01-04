namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class RateLimitedParallelAsyncProcessor<TResult> : AbstractAsyncProcessor<TResult>
{
    private readonly int _levelsOfParallelism;
    private Task _totalProgressTask;

    public RateLimitedParallelAsyncProcessor(List<Task<Task<TResult>>> initialTasks, int levelsOfParallelism,
        CancellationToken cancellationToken) : base(initialTasks, cancellationToken)
    {
        _levelsOfParallelism = levelsOfParallelism;
    }

    internal override Task Process()
    {
        return _totalProgressTask = Parallel.ForEachAsync(InitialTasks,
            new ParallelOptions { MaxDegreeOfParallelism = _levelsOfParallelism, CancellationToken = CancellationToken},
            async (task, token) =>
            {
                task.Start();
                await task.Unwrap();
            });
    }

    public override Task ContinuationTask => _totalProgressTask;
}

public class RateLimitedParallelAsyncProcessor : AbstractAsyncProcessor
{
    private readonly int _levelsOfParallelism;
    private Task _totalProgressTask;

    public RateLimitedParallelAsyncProcessor(List<Task<Task>> initialTasks, int levelsOfParallelism,
        CancellationToken cancellationToken) : base(initialTasks, cancellationToken)
    {
        _levelsOfParallelism = levelsOfParallelism;
    }

    internal override Task Process()
    {
        return _totalProgressTask = Parallel.ForEachAsync(InitialTasks,
            new ParallelOptions { MaxDegreeOfParallelism = _levelsOfParallelism, CancellationToken = CancellationToken},
            async (task, token) =>
            {
                task.Start();
                await task.Unwrap();
            });
    }

    public override Task Task => _totalProgressTask;
}