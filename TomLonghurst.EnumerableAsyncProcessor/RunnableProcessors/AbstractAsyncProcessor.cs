using TomLonghurst.EnumerableAsyncProcessor.Interfaces;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public abstract class AbstractAsyncProcessor<TResult> : IAsyncProcessor<TResult>, IDisposable
{
    protected readonly List<Task<Task<TResult>>> InitialTasks;
    private readonly CancellationTokenSource _cancellationTokenSource;
    protected CancellationToken CancellationToken => _cancellationTokenSource.Token;

    protected AbstractAsyncProcessor(List<Task<Task<TResult>>> initialTasks, CancellationTokenSource cancellationTokenSource)
    {
        InitialTasks = initialTasks;
        _cancellationTokenSource = cancellationTokenSource;

        cancellationTokenSource.Token.Register(Dispose);
        cancellationTokenSource.Token.ThrowIfCancellationRequested();
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
        _cancellationTokenSource.Cancel();
        ContinuationTask?.Dispose();
        _cancellationTokenSource.Dispose();
    }
}

public abstract class AbstractAsyncProcessor : IAsyncProcessor, IDisposable
{
    protected readonly List<Task<Task>> InitialTasks;
    private readonly CancellationTokenSource _cancellationTokenSource;
    protected CancellationToken CancellationToken => _cancellationTokenSource.Token;

    protected AbstractAsyncProcessor(List<Task<Task>> initialTasks, CancellationTokenSource cancellationTokenSource)
    {
        InitialTasks = initialTasks;
        _cancellationTokenSource = cancellationTokenSource;

        cancellationTokenSource.Token.Register(Dispose);
        cancellationTokenSource.Token.ThrowIfCancellationRequested();
    }
        
    internal abstract Task Process();
    
    public IEnumerable<Task> GetEnumerableTasks()
    {
        return InitialTasks.Select(x => x.Unwrap());
    }
    
    public abstract Task Task { get; }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        Task?.Dispose();
        _cancellationTokenSource.Dispose();
    }
}