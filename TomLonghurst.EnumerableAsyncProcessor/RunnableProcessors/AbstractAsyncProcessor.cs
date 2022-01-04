using TomLonghurst.EnumerableAsyncProcessor.Interfaces;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public abstract class AbstractAsyncProcessor<TResult> : IAsyncProcessor<TResult>, IDisposable
{
    protected readonly List<Task<Task<TResult>>> InitialTasks;
    protected readonly CancellationToken CancellationToken;

    protected AbstractAsyncProcessor(List<Task<Task<TResult>>> initialTasks, CancellationToken cancellationToken)
    {
        InitialTasks = initialTasks;
        CancellationToken = cancellationToken;

        cancellationToken.Register(Dispose);
        cancellationToken.ThrowIfCancellationRequested();
    }
        
    internal abstract Task Process();
    
    public IEnumerable<Task<TResult>> GetEnumerableTasks()
    {
        return InitialTasks.Select(x => x.Unwrap());
    }

    public async Task<IEnumerable<TResult>> GetResults()
    {
        return await Task.WhenAll(GetEnumerableTasks());
    }

    public abstract Task ContinuationTask { get; }

    public void Dispose()
    {
        ContinuationTask?.Dispose();
        InitialTasks.ForEach(x => x.Dispose());
    }
}

public abstract class AbstractAsyncProcessor : IAsyncProcessor
{
    protected readonly List<Task<Task>> InitialTasks;
    protected readonly CancellationToken CancellationToken;

    protected AbstractAsyncProcessor(List<Task<Task>> initialTasks, CancellationToken cancellationToken)
    {
        InitialTasks = initialTasks;
        CancellationToken = cancellationToken;

        cancellationToken.Register(() => initialTasks.ForEach(x => x.Dispose()));
    }
        
    internal abstract Task Process();
    
    public IEnumerable<Task> GetEnumerableTasks()
    {
        return InitialTasks.Select(x => x.Unwrap());
    }
    
    public abstract Task Task { get; }
}