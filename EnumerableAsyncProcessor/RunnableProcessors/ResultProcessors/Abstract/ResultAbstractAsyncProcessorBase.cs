using System.Runtime.CompilerServices;
using EnumerableAsyncProcessor.Interfaces;
using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

public abstract class ResultAbstractAsyncProcessorBase<TOutput> : IAsyncProcessor<TOutput>, IAsyncDisposable, IDisposable
{
    private static readonly TimeSpan DisposalTimeout = TimeSpan.FromSeconds(30);

    protected abstract IReadOnlyList<TaskCompletionSource<TOutput>> EnumerableTaskCompletionSources { get; }
    protected readonly CancellationToken CancellationToken;

    private readonly CancellationTokenSource _cancellationTokenSource;
    private CancellationTokenRegistration _cancellationTokenRegistration;
    private Task<TOutput[]>? _results;
    private Task? _processTask;
    private volatile bool _disposed;
    private readonly object _disposeLock = new();

    private Task<TOutput[]> Results => _results ??= Task.WhenAll(EnumerableTaskCompletionSources.Select(x => x.Task));

    protected ResultAbstractAsyncProcessorBase(CancellationTokenSource cancellationTokenSource)
    {
        ValidationHelper.ValidateCancellationTokenSource(cancellationTokenSource);

        _cancellationTokenSource = cancellationTokenSource;
        CancellationToken = cancellationTokenSource.Token;
    }

    internal abstract Task Process();

    // Cancellation is registered here rather than in the constructor so that a token cancelled
    // during construction can never invoke CancelAll on a partially constructed instance.
    internal void Start()
    {
        _cancellationTokenRegistration = CancellationToken.Register(CancelAll);
        _processTask = RunProcess();
    }

    private async Task RunProcess()
    {
        try
        {
            await Process().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            foreach (var taskCompletionSource in EnumerableTaskCompletionSources)
            {
                taskCompletionSource.TrySetCanceled(CancellationToken);
            }
        }
        catch (Exception exception)
        {
            // A failure outside the per-item wrappers (e.g. cancellation plumbing) would otherwise
            // leave awaiters of the per-item tasks hanging forever.
            foreach (var taskCompletionSource in EnumerableTaskCompletionSources)
            {
                taskCompletionSource.TrySetException(exception);
            }
        }
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
        if (_disposed)
            return;

        CancelAllCore();
    }

    private void CancelAllCore()
    {
        if (!_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
        }

        foreach (var taskCompletionSource in EnumerableTaskCompletionSources)
        {
            taskCompletionSource.TrySetCanceled(CancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        lock (_disposeLock)
        {
            if (_disposed)
                return;
            _disposed = true;
        }

        // Allow derived classes to dispose their resources first
        await DisposeAsyncCore().ConfigureAwait(false);

        CancelAllCore();
        _cancellationTokenRegistration.Dispose();

        // Give in-flight tasks a bounded window to observe cancellation and finish
        if (_processTask is { IsCompleted: false })
        {
            try
            {
                using var timeoutCts = new CancellationTokenSource(DisposalTimeout);
                await _processTask.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Timed out waiting for in-flight tasks - continue with disposal
            }
        }

        _cancellationTokenSource.Dispose();

        GC.SuppressFinalize(this);
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
#if NET6_0_OR_GREATER
        return ValueTask.CompletedTask;
#else
        return new ValueTask(Task.CompletedTask);
#endif
    }

    public void Dispose()
    {
        lock (_disposeLock)
        {
            if (_disposed)
                return;
            _disposed = true;
        }

        // Cancel and release without blocking; in-flight tasks complete against
        // already-cancelled completion sources, which is a no-op.
        CancelAllCore();
        _cancellationTokenRegistration.Dispose();
        _cancellationTokenSource.Dispose();

        GC.SuppressFinalize(this);
    }
}
