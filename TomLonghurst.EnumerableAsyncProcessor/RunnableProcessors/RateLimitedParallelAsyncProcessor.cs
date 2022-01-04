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
        return _totalProgressTask = Parallel.ForEachAsync(_initialTasks,
            new ParallelOptions { MaxDegreeOfParallelism = _levelsOfParallelism, CancellationToken = _cancellationToken},
            async (task, token) =>
            {
                task.Start();
                await task.Unwrap();
            });
    }

    public override Task GetOverallProgressTask()
    {
        return _totalProgressTask;
    }
}