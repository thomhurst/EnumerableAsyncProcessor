using EnumerableAsyncProcessor.Interfaces;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable.ResultProcessors;

/// <summary>
/// Sequential processor that processes items one at a time and returns results in order.
/// </summary>
public sealed class ResultAsyncEnumerableOneAtATimeProcessor<TInput, TOutput> : IAsyncEnumerableProcessor<TOutput>
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task<TOutput>> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private int _disposed;
    private int _executionState;
    private TaskCompletionSource? _executionCompleted;

    internal ResultAsyncEnumerableOneAtATimeProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task<TOutput>> taskSelector,
        CancellationTokenSource cancellationTokenSource)
    {
        _items = items;
        _taskSelector = taskSelector;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public IAsyncEnumerable<TOutput> ExecuteAsync()
    {
        StreamingExecution.GuardSingleUse(ref _executionState, ref _disposed, this);
        return ExecuteCoreAsync();
    }

    private async IAsyncEnumerable<TOutput> ExecuteCoreAsync()
    {
        var executionCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _executionCompleted = executionCompleted;
        var cancellationToken = _cancellationTokenSource.Token;

        try
        {
            await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                var result = await _taskSelector(item).ConfigureAwait(false);
                yield return result;
            }
        }
        finally
        {
            DisposeCancellationSource(cancelFirst: false);
            executionCompleted.TrySetResult();
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

        // Mirror ProcessorLifecycle: give an in-flight enumeration a bounded window to observe cancellation.
        if (_executionCompleted is { Task.IsCompleted: false } executionCompleted)
        {
            try
            {
                await executionCompleted.Task.WaitAsync(ProcessorLifecycle.DisposalTimeout).ConfigureAwait(false);
            }
            catch
            {
                // Timeout of the in-flight enumeration; disposal must not throw.
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
