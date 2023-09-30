using System.Runtime.CompilerServices;
using EnumerableAsyncProcessor.Interfaces;

namespace EnumerableAsyncProcessor.RunnableProcessors.Abstract;

public abstract class AbstractAsyncProcessorBase : IAsyncProcessor, IDisposable
{
    protected readonly List<TaskCompletionSource> EnumerableTaskCompletionSources;
    protected readonly CancellationToken CancellationToken;

    private readonly List<Task> _enumerableTasks;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _overallTask;


    protected AbstractAsyncProcessorBase(int count, CancellationTokenSource cancellationTokenSource)
    {
        EnumerableTaskCompletionSources = Enumerable.Range(0, count).Select(_ => new TaskCompletionSource()).ToList();
        _enumerableTasks = EnumerableTaskCompletionSources.Select(x => x.Task).ToList();
        _overallTask = Task.WhenAll(_enumerableTasks);
        
        _cancellationTokenSource = cancellationTokenSource;
        
        CancellationToken = cancellationTokenSource.Token;
        CancellationToken.Register(Dispose);
        CancellationToken.ThrowIfCancellationRequested();
    }

    internal abstract Task Process();
    
    public IEnumerable<Task> GetEnumerableTasks()
    {
        return _enumerableTasks;
    }

    public TaskAwaiter GetAwaiter()
    {
        return WaitAsync().GetAwaiter();
    }

    public Task WaitAsync()
    {
        return _overallTask;
    }
    
    public void CancelAll()
    {
        if (!_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
        }

        EnumerableTaskCompletionSources.ForEach(t => t.TrySetCanceled(CancellationToken));
        
        _cancellationTokenSource.Dispose();
    }

    public void Dispose()
    {
        CancelAll();
        GC.SuppressFinalize(this);
    }
}