using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor;

/// <summary>
/// Owns the start/cancel/dispose lifecycle shared by the void and result processor base classes,
/// which cannot share a common ancestor because they fan out to differently typed completion sources.
/// </summary>
internal sealed class ProcessorLifecycle
{
    internal static readonly TimeSpan DisposalTimeout = TimeSpan.FromSeconds(30);

    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Action _trySetCanceledAll;
    private readonly Action<Exception> _trySetExceptionAll;
    private CancellationTokenRegistration _cancellationTokenRegistration;
    private Task? _processTask;
    private volatile bool _disposed;
    private readonly object _disposeLock = new();

    public CancellationToken Token { get; }

    public ProcessorLifecycle(CancellationTokenSource cancellationTokenSource, Action trySetCanceledAll, Action<Exception> trySetExceptionAll)
    {
        ValidationHelper.ValidateCancellationTokenSource(cancellationTokenSource);

        _cancellationTokenSource = cancellationTokenSource;
        _trySetCanceledAll = trySetCanceledAll;
        _trySetExceptionAll = trySetExceptionAll;
        Token = cancellationTokenSource.Token;
    }

    // Cancellation is registered here rather than at construction so that a token cancelled
    // while the processor is still being constructed can never fire on a partially built instance.
    public void Start(Func<Task> process)
    {
        _cancellationTokenRegistration = Token.Register(CancelAll);
        _processTask = RunProcess(process);
    }

    private async Task RunProcess(Func<Task> process)
    {
        try
        {
            await process().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _trySetCanceledAll();
        }
        catch (Exception exception)
        {
            // A failure outside the per-item wrappers (e.g. cancellation plumbing) would otherwise
            // leave awaiters of the per-item tasks hanging forever.
            _trySetExceptionAll(exception);
        }
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

        _trySetCanceledAll();
    }

    public async ValueTask DisposeAsync(Func<ValueTask> disposeAsyncCore)
    {
        lock (_disposeLock)
        {
            if (_disposed)
                return;
            _disposed = true;
        }

        // Allow the owning processor to dispose its resources first
        await disposeAsyncCore().ConfigureAwait(false);

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
    }
}
