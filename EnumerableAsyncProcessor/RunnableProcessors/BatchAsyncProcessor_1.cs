using EnumerableAsyncProcessor.RunnableProcessors.Abstract;
using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class BatchAsyncProcessor<TInput> : AbstractAsyncProcessor<TInput>
{
    private readonly int _batchSize;

    internal BatchAsyncProcessor(int batchSize, IEnumerable<TInput> items, Func<TInput, Task> taskSelector,
        CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
        ValidationHelper.ValidateBatchSize(batchSize);

        _batchSize = batchSize;
    }
    
    internal override async Task Process()
    {
        var batchedItems = TaskWrappers.Chunk(_batchSize);
        
        foreach (var currentBatch in batchedItems)
        {
            await ProcessBatch(currentBatch).ConfigureAwait(false);
        }
    }

    private async Task ProcessBatch(ItemTaskWrapper<TInput>[] currentBatch)
    {
        await Task.WhenAll(currentBatch.Select(tw => tw.Process(CancellationToken))).ConfigureAwait(false);
    }
}