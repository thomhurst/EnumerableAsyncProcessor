using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EnumerableAsyncProcessor.Interfaces;

namespace EnumerableAsyncProcessor.RunnableProcessors.Abstract;

public abstract class AbstractAsyncProcessorBase : IAsyncProcessor, IDisposable
{
    protected abstract IEnumerable<TaskCompletionSource> EnumerableTaskCompletionSources { get; }
    protected readonly CancellationToken CancellationToken;

    [field: MaybeNull, AllowNull]
    private IEnumerable<Task> EnumerableTasks => field ??= EnumerableTaskCompletionSources.Select(x => x.Task);
    
    private readonly CancellationTokenSource _cancellationTokenSource;

    [field: AllowNull, MaybeNull]
    private Task OverallTask  => field ??= Task.WhenAll(EnumerableTasks);
    
    protected AbstractAsyncProcessorBase(CancellationTokenSource cancellationTokenSource)
    {
        CancellationToken = cancellationTokenSource.Token;
        CancellationToken.Register(Dispose);
        CancellationToken.ThrowIfCancellationRequested();
        
        _cancellationTokenSource = cancellationTokenSource;
    }

    internal abstract Task Process();
    
    public IEnumerable<Task> GetEnumerableTasks()
    {
        return EnumerableTasks;
    }

    public TaskAwaiter GetAwaiter()
    {
        return WaitAsync().GetAwaiter();
    }

    public Task WaitAsync()
    {
        return OverallTask;
    }
    
    public void CancelAll()
    {
        if (!_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
        }

        foreach (var tcs in EnumerableTaskCompletionSources)
        {
            tcs.TrySetCanceled(CancellationToken);
        }
        
        _cancellationTokenSource.Dispose();
    }

    public void Dispose()
    {
        CancelAll();
        GC.SuppressFinalize(this);
    }
}