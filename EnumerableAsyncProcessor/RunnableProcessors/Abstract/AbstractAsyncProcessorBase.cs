using System.Runtime.CompilerServices;
using EnumerableAsyncProcessor.Interfaces;

namespace EnumerableAsyncProcessor.RunnableProcessors.Abstract;

public abstract class AbstractAsyncProcessorBase : IAsyncProcessor, IAsyncDisposable, IDisposable
{
    protected abstract IReadOnlyList<TaskCompletionSource> EnumerableTaskCompletionSources { get; }
    protected readonly CancellationToken CancellationToken;

    private readonly ProcessorLifecycle _lifecycle;
    private Task? _overallTask;

    private Task OverallTask => _overallTask ??= Task.WhenAll(GetEnumerableTasks());

    protected AbstractAsyncProcessorBase(CancellationTokenSource cancellationTokenSource)
    {
        _lifecycle = new ProcessorLifecycle(cancellationTokenSource, TrySetCanceledAll, TrySetExceptionAll);
        CancellationToken = _lifecycle.Token;
    }

    internal abstract Task Process();

    internal IAsyncProcessor StartProcessing()
    {
        _lifecycle.Start(Process);
        return this;
    }

    public IEnumerable<Task> GetEnumerableTasks()
    {
        return EnumerableTaskCompletionSources.Select(x => x.Task);
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
