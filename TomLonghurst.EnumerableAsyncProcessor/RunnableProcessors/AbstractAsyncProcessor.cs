using TomLonghurst.EnumerableAsyncProcessor.Extensions;
using TomLonghurst.EnumerableAsyncProcessor.Interfaces;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public abstract class AbstractAsyncProcessor<TResult> : IAsyncProcessor<TResult>, IDisposable
{
    protected readonly List<Task<Task<TResult>>> InitialTasks;
    protected readonly List<Task<TResult>> UnwrappedTasks;
    private readonly CancellationTokenSource _cancellationTokenSource;
    protected CancellationToken CancellationToken => _cancellationTokenSource.Token;


    protected AbstractAsyncProcessor(List<Task<Task<TResult>>> initialTasks, CancellationTokenSource cancellationTokenSource)
    {
        InitialTasks = initialTasks;
        UnwrappedTasks = InitialTasks.Select(x => x.Unwrap()).ToList();
        _cancellationTokenSource = cancellationTokenSource;

        cancellationTokenSource.Token.Register(Dispose);
        cancellationTokenSource.Token.ThrowIfCancellationRequested();
    }

    internal abstract Task Process();
    
    public IEnumerable<Task<TResult>> GetEnumerableTasks()
    {
        return UnwrappedTasks;
    }

    public async Task<IEnumerable<TResult>> GetResults()
    {
        return await Task.WhenAll(GetEnumerableTasks());
    }

    public abstract Task ContinuationTask { get; }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();

        InitialTasks.ForEach(t => t.TryStart());
    }
}

public abstract class AbstractAsyncProcessor : IAsyncProcessor, IDisposable
{
    protected readonly List<Task<Task>> InitialTasks;
    protected readonly List<Task> UnwrappedTasks;
    private readonly CancellationTokenSource _cancellationTokenSource;
    protected readonly CancellationToken CancellationToken;

    protected AbstractAsyncProcessor(List<Task<Task>> initialTasks, CancellationTokenSource cancellationTokenSource)
    {
        InitialTasks = initialTasks;
        UnwrappedTasks = InitialTasks.Select(x => x.Unwrap()).ToList();
        
        _cancellationTokenSource = cancellationTokenSource;
        CancellationToken = cancellationTokenSource.Token;

        cancellationTokenSource.Token.Register(Dispose);
        cancellationTokenSource.Token.ThrowIfCancellationRequested();
    }
        
    internal abstract Task Process();
    
    public IEnumerable<Task> GetEnumerableTasks()
    {
        return UnwrappedTasks;
    }
    
    public abstract Task Task { get; }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        
        InitialTasks.ForEach(t => t.TryStart());
    }
}