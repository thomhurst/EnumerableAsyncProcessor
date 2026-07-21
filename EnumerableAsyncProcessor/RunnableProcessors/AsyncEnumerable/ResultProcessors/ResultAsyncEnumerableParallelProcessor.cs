using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.Interfaces;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable.ResultProcessors;

/// <summary>
/// Streams one result per item from an <see cref="IAsyncEnumerable{T}"/> source, processing in
/// parallel. Bounded (<c>maxConcurrency</c> set) runs yield results in source order with source
/// backpressure; unbounded runs yield results in completion order. Abandoning the stream early
/// cancels remaining work.
/// </summary>
public sealed class ResultAsyncEnumerableParallelProcessor<TInput, TOutput> : IAsyncEnumerableProcessor<TOutput>
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task<TOutput>> _taskSelector;
    private readonly int? _maxConcurrency;
    private readonly bool _scheduleOnThreadPool;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private int _disposed;
    private int _executionState;
    private TaskCompletionSource? _executionCompleted;

    internal ResultAsyncEnumerableParallelProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task<TOutput>> taskSelector,
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
        var completedNormally = false;

        try
        {
            if (_maxConcurrency.HasValue)
            {
                Func<TInput, Task<TOutput>> taskSelector = _scheduleOnThreadPool
                    ? item => Task.Run(() => _taskSelector(item), cancellationToken)
                    : _taskSelector;

                // Enumerated manually so that on abandonment or failure the processor's token is
                // cancelled BEFORE the pool enumerator's disposal drains its in-flight work -
                // token-aware selectors then abort instead of being waited on to natural completion.
                var enumerator = AsyncEnumerableWorkerPool.ProcessResultsAsync(
                        _items,
                        taskSelector,
                        _maxConcurrency.Value,
                        cancellationToken)
                    .GetAsyncEnumerator(CancellationToken.None);

                try
                {
                    while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        yield return enumerator.Current;
                    }

                    completedNormally = true;
                }
                finally
                {
                    if (!completedNormally)
                    {
                        CancelForDisposal();
                    }

                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }
            }
            else
            {
                // Unbounded parallel processing
                var tasks = new List<Task<TOutput>>();

                try
                {
                    await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
                    {
                        var capturedItem = item;

                        tasks.Add(_scheduleOnThreadPool
                            ? Task.Run(() => _taskSelector(capturedItem), cancellationToken)
                            : _taskSelector(capturedItem));
                    }

                    // Yield all results as they complete
                    await foreach (var result in tasks.ToIAsyncEnumerable(cancellationToken).ConfigureAwait(false))
                    {
                        yield return result;
                    }

                    completedNormally = true;
                }
                finally
                {
                    if (!completedNormally && tasks.Count > 0)
                    {
                        // Abandonment (early break) or a propagating failure: cancel in-flight
                        // work, drain it within the disposal window rather than for as long as it
                        // takes, and observe its failures so they can neither mask the propagating
                        // exception nor surface as UnobservedTaskException.
                        CancelForDisposal();

                        try
                        {
                            await Task.WhenAll(tasks).WaitAsync(ProcessorLifecycle.DisposalTimeout).ConfigureAwait(false);
                        }
                        catch
                        {
                            // Failures are observed below; the propagating exception stays primary.
                        }

                        StreamingExecution.ObserveFailures(tasks);
                    }
                }
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
