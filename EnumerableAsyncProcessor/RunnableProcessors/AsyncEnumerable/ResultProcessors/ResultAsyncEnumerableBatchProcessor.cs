using System.Runtime.CompilerServices;
using EnumerableAsyncProcessor.Interfaces;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable.ResultProcessors;

public sealed class ResultAsyncEnumerableBatchProcessor<TInput, TOutput> : IAsyncEnumerableProcessor<TOutput>
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task<TOutput>> _taskSelector;
    private readonly int _batchSize;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private int _disposed;

    internal ResultAsyncEnumerableBatchProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task<TOutput>> taskSelector,
        int batchSize,
        CancellationTokenSource cancellationTokenSource)
    {
        _items = items;
        _taskSelector = taskSelector;
        _batchSize = batchSize;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public async IAsyncEnumerable<TOutput> ExecuteAsync()
    {
        var cancellationToken = _cancellationTokenSource.Token;

        try
        {
            var batch = new List<TInput>(_batchSize);

            await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                batch.Add(item);

                if (batch.Count >= _batchSize)
                {
                    await foreach (var result in ProcessBatch(batch, cancellationToken).ConfigureAwait(false))
                    {
                        yield return result;
                    }
                    batch = new List<TInput>(_batchSize);
                }
            }

            // Process any remaining items in the final batch
            if (batch.Count > 0)
            {
                await foreach (var result in ProcessBatch(batch, cancellationToken).ConfigureAwait(false))
                {
                    yield return result;
                }
            }
        }
        finally
        {
            DisposeCancellationSource(cancelFirst: false);
        }
    }

    private async IAsyncEnumerable<TOutput> ProcessBatch(List<TInput> batch, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tasks = batch.Select(item => _taskSelector(item)).ToArray();
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        foreach (var result in results)
        {
            yield return result;
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

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
