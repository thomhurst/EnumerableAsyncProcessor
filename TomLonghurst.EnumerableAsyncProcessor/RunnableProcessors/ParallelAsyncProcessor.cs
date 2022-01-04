namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class ParallelAsyncProcessor<TResult> : AbstractAsyncProcessor<TResult>
{
    private Task _totalProgressTask;

    public ParallelAsyncProcessor(List<Task<Task<TResult>>> initialTasks, CancellationToken cancellationToken) : base(initialTasks, cancellationToken)
    {
        cancellationToken.Register(() => initialTasks.ForEach(x => x.Dispose()));
    }

    internal override Task Process()
    {
        return _totalProgressTask = Parallel.ForEachAsync(_initialTasks,
            new ParallelOptions { MaxDegreeOfParallelism = -1, CancellationToken = _cancellationToken },
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