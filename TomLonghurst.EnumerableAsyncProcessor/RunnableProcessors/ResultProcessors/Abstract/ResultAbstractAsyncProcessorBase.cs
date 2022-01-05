using System.Runtime.CompilerServices;
using TomLonghurst.EnumerableAsyncProcessor.Interfaces;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

public abstract class ResultAbstractAsyncProcessorBase<TResult> : IAsyncProcessor<TResult>, IDisposable
{
    protected readonly List<TaskCompletionSource<TResult>> EnumerableTaskCompletionSources;
    protected readonly CancellationToken CancellationToken;

    private readonly List<Task<TResult>> _enumerableTasks;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task<TResult[]> _results;


    protected ResultAbstractAsyncProcessorBase(int count, CancellationTokenSource cancellationTokenSource)
    {
        EnumerableTaskCompletionSources = Enumerable.Range(0, count).Select(_ => new TaskCompletionSource<TResult>()).ToList();
        _enumerableTasks = EnumerableTaskCompletionSources.Select(x => x.Task).ToList();
        _results = Task.WhenAll(_enumerableTasks);
        
        _cancellationTokenSource = cancellationTokenSource;
        CancellationToken = cancellationTokenSource.Token;

        cancellationTokenSource.Token.Register(Dispose);
        cancellationTokenSource.Token.ThrowIfCancellationRequested();
    }

    internal abstract Task Process();
    
    public IEnumerable<Task<TResult>> GetEnumerableTasks()
    {
        return _enumerableTasks;
    }

    public Task<TResult[]> GetResults()
    {
        return _results;
    }

    public TaskAwaiter<TResult[]> GetAwaiter()
    {
        return GetResults().GetAwaiter();
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        EnumerableTaskCompletionSources.ForEach(t => t.TrySetCanceled(CancellationToken));
        _cancellationTokenSource.Dispose();
        GC.SuppressFinalize(this);
    }
}