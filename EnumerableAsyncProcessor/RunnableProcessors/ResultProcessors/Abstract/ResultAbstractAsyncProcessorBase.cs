using System.Runtime.CompilerServices;
using EnumerableAsyncProcessor.Interfaces;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

public abstract class ResultAbstractAsyncProcessorBase<TOutput> : IAsyncProcessor<TOutput>, IAsyncDisposable, IDisposable
{
    protected abstract IReadOnlyList<TaskCompletionSource<TOutput>> EnumerableTaskCompletionSources { get; }
    protected readonly CancellationToken CancellationToken;

    private readonly ProcessorLifecycle _lifecycle;
    private Task<TOutput[]>? _results;

    private Task<TOutput[]> Results => _results ??= Task.WhenAll(GetEnumerableTasks());

    protected ResultAbstractAsyncProcessorBase(CancellationTokenSource cancellationTokenSource)
    {
        _lifecycle = new ProcessorLifecycle(cancellationTokenSource, TrySetCanceledAll, TrySetExceptionAll);
        CancellationToken = _lifecycle.Token;
    }

    internal abstract Task Process();

    internal IAsyncProcessor<TOutput> StartProcessing()
    {
        _lifecycle.Start(Process);
        return this;
    }

    public IEnumerable<Task<TOutput>> GetEnumerableTasks()
    {
        return EnumerableTaskCompletionSources.Select(x => x.Task);
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
        _lifecycle.CancelAll();
    }

    private void TrySetCanceledAll()
    {
        foreach (var taskCompletionSource in EnumerableTaskCompletionSources)
        {
            taskCompletionSource.TrySetCanceled(CancellationToken);
        }
    }

    private void TrySetExceptionAll(Exception exception)
    {
        foreach (var taskCompletionSource in EnumerableTaskCompletionSources)
        {
            taskCompletionSource.TrySetException(exception);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _lifecycle.DisposeAsync(DisposeAsyncCore).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        _lifecycle.Dispose();
        GC.SuppressFinalize(this);
    }
}
