using System.Runtime.CompilerServices;
using TomLonghurst.EnumerableAsyncProcessor.Extensions;
using TomLonghurst.EnumerableAsyncProcessor.Interfaces;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

public abstract class ResultAbstractAsyncProcessorBase<TOutput> : IAsyncProcessor<TOutput>, IDisposable
{
    protected readonly List<TaskCompletionSource<TOutput>> EnumerableTaskCompletionSources;
    protected readonly CancellationToken CancellationToken;

    private readonly List<Task<TOutput>> _enumerableTasks;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task<TOutput[]> _results;


    protected ResultAbstractAsyncProcessorBase(int count, CancellationTokenSource cancellationTokenSource)
    {
        EnumerableTaskCompletionSources = Enumerable.Range(0, count).Select(_ => new TaskCompletionSource<TOutput>()).ToList();
        _enumerableTasks = EnumerableTaskCompletionSources.Select(x => x.Task).ToList();
        _results = Task.WhenAll(_enumerableTasks);
        
        _cancellationTokenSource = cancellationTokenSource;
        CancellationToken = cancellationTokenSource.Token;

        cancellationTokenSource.Token.Register(Dispose);
        cancellationTokenSource.Token.ThrowIfCancellationRequested();
    }

    internal abstract Task Process();
    
    public IEnumerable<Task<TOutput>> GetEnumerableTasks()
    {
        return _enumerableTasks;
    }

    public Task<TOutput[]> GetResultsAsync()
    {
        return _results;
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

        EnumerableTaskCompletionSources.ForEach(t => t.TrySetCanceled(CancellationToken));
        
        _cancellationTokenSource.Dispose();
    }

    public void Dispose()
    {
        CancelAll();
        GC.SuppressFinalize(this);
    }
}