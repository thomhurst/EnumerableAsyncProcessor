using EnumerableAsyncProcessor.Interfaces;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable;

/// <summary>
/// Sequential processor that processes items one at a time from an IAsyncEnumerable.
/// </summary>
public sealed class AsyncEnumerableOneAtATimeProcessor<TInput> : IAsyncEnumerableProcessor
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private int _disposed;
    private int _executionState;
    private Task? _executionTask;

    internal AsyncEnumerableOneAtATimeProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task> taskSelector,
        CancellationTokenSource cancellationTokenSource)
    {
        _items = items;
        _taskSelector = taskSelector;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public Task ExecuteAsync()
    {
        StreamingExecution.GuardSingleUse(ref _executionState, Volatile.Read(ref _disposed), this);

        // A TaskCompletionSource rather than the async method's own task, so a multi-fault
        // item task surfaces every inner exception instead of only the first one awaited.
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _executionTask = completionSource.Task;
        _ = ExecuteCoreAsync(completionSource);
        return completionSource.Task;
    }

    private async Task ExecuteCoreAsync(TaskCompletionSource completionSource)
    {
        var exceptions = new List<Exception>();
        var wasCanceled = false;
        var cancellationToken = CancellationToken.None;

        try
        {
            cancellationToken = _cancellationTokenSource.Token;

            await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                var task = _taskSelector(item);

                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException || task.IsFaulted)
                {
                    StreamingExecution.CollectFailures(task, exception, exceptions, ref wasCanceled);
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            wasCanceled = true;
        }
        catch (Exception exception)
        {
            exceptions.Add(exception);
        }
        finally
        {
            DisposeCancellationSource(cancelFirst: false);
            StreamingExecution.Complete(completionSource, exceptions, wasCanceled, cancellationToken);
        }
    }

    public void Dispose()
    {
        DisposeCancellationSource(cancelFirst: true);
    }

    // Explicit disposal cancels in-flight work first; the completion path has nothing left to cancel.
    private void DisposeCancellationSource(bool cancelFirst)
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        if (cancelFirst)
        {
            _cancellationTokenSource.Cancel();
        }

        _cancellationTokenSource.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        CancelForDisposal();

        // Mirror ProcessorLifecycle: give the in-flight run a bounded window to observe cancellation.
        if (_executionTask is { IsCompleted: false } executionTask)
        {
            try
            {
                await executionTask.WaitAsync(ProcessorLifecycle.DisposalTimeout).ConfigureAwait(false);
            }
            catch
            {
                // Cancellation, failure, or timeout of the in-flight run; disposal must not throw.
            }
        }

        DisposeCancellationSource(cancelFirst: false);
    }

    private void CancelForDisposal()
    {
        try
        {
            if (Volatile.Read(ref _disposed) == 0)
            {
                _cancellationTokenSource.Cancel();
            }
        }
        catch (ObjectDisposedException)
        {
            // The run completed and disposed the source concurrently - nothing left to cancel.
        }
    }
}
