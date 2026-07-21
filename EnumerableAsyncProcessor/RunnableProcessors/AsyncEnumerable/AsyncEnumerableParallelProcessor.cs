using EnumerableAsyncProcessor.Interfaces;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable;

public sealed class AsyncEnumerableParallelProcessor<TInput> : IAsyncEnumerableProcessor
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task> _taskSelector;
    private readonly int? _maxConcurrency;
    private readonly bool _scheduleOnThreadPool;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private int _disposed;
    private int _executionState;
    private Task? _executionTask;

    internal AsyncEnumerableParallelProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task> taskSelector,
        int? maxConcurrency,
        bool scheduleOnThreadPool,
        CancellationTokenSource cancellationTokenSource)
    {
        _items = items;
        _taskSelector = taskSelector;
        _maxConcurrency = maxConcurrency;
        _scheduleOnThreadPool = scheduleOnThreadPool;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public Task ExecuteAsync()
    {
        StreamingExecution.GuardSingleUse(ref _executionState, Volatile.Read(ref _disposed), this);

        // A TaskCompletionSource rather than the async method's own task, so the returned task
        // carries every failure (Task.WhenAll fidelity) instead of only the first one awaited.
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

            if (_maxConcurrency.HasValue)
            {
                Func<TInput, Task> taskSelector = _scheduleOnThreadPool
                    ? item => Task.Run(() => _taskSelector(item), cancellationToken)
                    : _taskSelector;

                var poolTask = AsyncEnumerableWorkerPool.ProcessAsync(
                    _items,
                    taskSelector,
                    _maxConcurrency.Value,
                    cancellationToken);

                try
                {
                    await poolTask.ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    StreamingExecution.CollectFailures(poolTask, exception, exceptions, ref wasCanceled);
                }
            }
            else
            {
                // Unbounded parallel processing
                var tasks = new List<Task>();

                try
                {
                    await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
                    {
                        var capturedItem = item;

                        tasks.Add(_scheduleOnThreadPool
                            ? Task.Run(() => _taskSelector(capturedItem), cancellationToken)
                            : _taskSelector(capturedItem));
                    }
                }
                catch (OperationCanceledException)
                {
                    wasCanceled = true;
                }
                catch (Exception exception)
                {
                    // Recorded before the drain below so a mid-enumeration source failure stays
                    // the primary exception; started-task failures append rather than replace it.
                    exceptions.Add(exception);
                }

                if (tasks.Count > 0)
                {
                    var whenAll = Task.WhenAll(tasks);

                    try
                    {
                        await whenAll.ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        StreamingExecution.CollectFailures(whenAll, exception, exceptions, ref wasCanceled);
                    }
                }
            }
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
