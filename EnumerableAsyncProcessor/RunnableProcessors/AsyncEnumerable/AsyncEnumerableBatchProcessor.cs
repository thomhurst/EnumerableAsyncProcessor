using EnumerableAsyncProcessor.Interfaces;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable;

public sealed class AsyncEnumerableBatchProcessor<TInput> : IAsyncEnumerableProcessor
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task> _taskSelector;
    private readonly int _batchSize;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private int _disposed;
    private int _executionState;
    private Task? _executionTask;

    internal AsyncEnumerableBatchProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task> taskSelector,
        int batchSize,
        CancellationTokenSource cancellationTokenSource)
    {
        _items = items;
        _taskSelector = taskSelector;
        _batchSize = batchSize;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public Task ExecuteAsync()
    {
        StreamingExecution.GuardSingleUse(ref _executionState, Volatile.Read(ref _disposed), this);

        // A TaskCompletionSource rather than the async method's own task, so the returned task
        // carries every failure from a failing batch (Task.WhenAll fidelity) instead of only
        // the first one awaited.
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
            var batch = new List<TInput>(_batchSize);

            await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                batch.Add(item);

                if (batch.Count >= _batchSize)
                {
                    if (!await ProcessBatch(batch, exceptions).ConfigureAwait(false))
                    {
                        break;
                    }

                    batch = new List<TInput>(_batchSize);
                }
            }

            // Process any remaining items in the final batch
            if (exceptions.Count == 0 && batch.Count > 0)
            {
                await ProcessBatch(batch, exceptions).ConfigureAwait(false);
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

    // Returns false when the batch failed, which stops subsequent batches (a failing batch has
    // always halted the run); every failure in the batch is collected, not just the first.
    private async Task<bool> ProcessBatch(List<TInput> batch, List<Exception> exceptions)
    {
        var tasks = batch.Select(item => _taskSelector(item)).ToArray();
        var whenAll = Task.WhenAll(tasks);

        try
        {
            await whenAll.ConfigureAwait(false);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || whenAll.IsFaulted)
        {
            var wasCanceled = false;
            StreamingExecution.CollectFailures(whenAll, exception, exceptions, ref wasCanceled);
            return false;
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
