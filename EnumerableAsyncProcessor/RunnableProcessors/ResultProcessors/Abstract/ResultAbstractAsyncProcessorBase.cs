using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EnumerableAsyncProcessor.Interfaces;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

public abstract class ResultAbstractAsyncProcessorBase<TOutput> : IAsyncProcessor<TOutput>, IDisposable
{
    protected abstract IEnumerable<TaskCompletionSource<TOutput>> EnumerableTaskCompletionSources { get; }
    protected readonly CancellationToken CancellationToken;

    [field: MaybeNull, AllowNull]
    private IEnumerable<Task<TOutput>> EnumerableTasks => field ??= EnumerableTaskCompletionSources.Select(x => x.Task);
    
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    [field: AllowNull, MaybeNull]
    private Task<TOutput[]> Results  => field ??= Task.WhenAll(EnumerableTasks);


    protected ResultAbstractAsyncProcessorBase(CancellationTokenSource cancellationTokenSource)
    {
        _cancellationTokenSource = cancellationTokenSource;
        
        CancellationToken = cancellationTokenSource.Token;
        CancellationToken.Register(Dispose);
        CancellationToken.ThrowIfCancellationRequested();
    }

    internal abstract Task Process();
    
    public IEnumerable<Task<TOutput>> GetEnumerableTasks()
    {
        return EnumerableTasks;
    }

    public Task<TOutput[]> GetResultsAsync()
    {
        return Results;
    }

    public IAsyncEnumerable<TOutput> GetResultsAsyncEnumerable()
    {
        return GetEnumerableTasks().ToIAsyncEnumerable();
    }

    public TaskAwaiter<TOutput[]> GetAwaiter()
    {
        return GetResultsAsync().GetAwaiter();
    }

    public void CancelAll()
    {
        if (!_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
        }

        foreach (var taskCompletionSource in EnumerableTaskCompletionSources)
        {
            taskCompletionSource.TrySetCanceled(CancellationToken);
        }
        
        _cancellationTokenSource.Dispose();
    }

    public void Dispose()
    {
        CancelAll();
        GC.SuppressFinalize(this);
    }
}