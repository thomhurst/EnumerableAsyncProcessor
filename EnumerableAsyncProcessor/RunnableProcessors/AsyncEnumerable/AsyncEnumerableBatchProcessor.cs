using EnumerableAsyncProcessor.Interfaces;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable;

public sealed class AsyncEnumerableBatchProcessor<TInput> : IAsyncEnumerableProcessor
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task> _taskSelector;
    private readonly int _batchSize;
    private readonly CancellationTokenSource _cancellationTokenSource;

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

    public async Task ExecuteAsync()
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
                    await ProcessBatch(batch).ConfigureAwait(false);
                    batch = new List<TInput>(_batchSize);
                }
            }

            // Process any remaining items in the final batch
            if (batch.Count > 0)
            {
                await ProcessBatch(batch).ConfigureAwait(false);
            }
        }
        finally
        {
            _cancellationTokenSource.Dispose();
        }
    }

    private async Task ProcessBatch(List<TInput> batch)
    {
        var tasks = batch.Select(item => _taskSelector(item)).ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
