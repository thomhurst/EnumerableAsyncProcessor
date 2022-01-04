namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class ParallelAsyncProcessor<TResult> : AbstractAsyncProcessor<TResult>
{
    private Task _totalProgressTask;

    public ParallelAsyncProcessor(List<Task<Task<TResult>>> initialTasks, CancellationTokenSource cancellationTokenSource) : base(initialTasks, cancellationTokenSource)
    {
    }

    internal override Task Process()
    {
        _totalProgressTask = Task.WhenAll(UnwrappedTasks);
        
        return Parallel.ForEachAsync(InitialTasks,
            new ParallelOptions { MaxDegreeOfParallelism = -1, CancellationToken = CancellationToken },
            async (task, token) =>
            {
                task.Start();
                await task.Unwrap();
            });
    }

    public override Task ContinuationTask => _totalProgressTask;
}

public class ParallelAsyncProcessor : AbstractAsyncProcessor
{
    private Task _totalProgressTask;

    public ParallelAsyncProcessor(List<Task<Task>> initialTasks, CancellationTokenSource cancellationTokenSource) : base(initialTasks, cancellationTokenSource)
    {
    }

    internal override Task Process()
    {
        _totalProgressTask = Task.WhenAll(UnwrappedTasks);
        
        return  Parallel.ForEachAsync(InitialTasks,
            new ParallelOptions { MaxDegreeOfParallelism = -1, CancellationToken = CancellationToken },
            async (task, token) =>
            {
                task.Start();
                await task.Unwrap();
            });
    }

    public override Task Task => _totalProgressTask;
}