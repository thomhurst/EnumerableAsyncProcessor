#if NET6_0_OR_GREATER
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable.ResultProcessors;

public class ResultAsyncEnumerableBatchProcessor<TInput, TOutput> : IAsyncEnumerableProcessor<TOutput>
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task<TOutput>> _taskSelector;
    private readonly int _batchSize;
    private readonly CancellationTokenSource _cancellationTokenSource;

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
    
    private async IAsyncEnumerable<TOutput> ProcessBatch(List<TInput> batch, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tasks = batch.Select(item => _taskSelector(item)).ToArray();
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        
        foreach (var result in results)
        {
            yield return result;
        }
    }
}
#endif