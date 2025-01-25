using System.Collections.Immutable;
using EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class BatchAsyncProcessor<TInput> : AbstractAsyncProcessor<TInput>
{
    private readonly int _batchSize;

    internal BatchAsyncProcessor(int batchSize, IEnumerable<TInput> items, Func<TInput, Task> taskSelector,
        CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
        _batchSize = batchSize;
    }
    
    internal override async Task Process()
    {
        var batchedItems = TaskWrappers.Chunk(_batchSize);
        
        foreach (var currentBatch in batchedItems)
        {
            await ProcessBatch(currentBatch);
        }
    }

    private Task ProcessBatch(ItemTaskWrapper<TInput>[] currentBatch)
    {
        foreach (var currentItem in currentBatch)
        {
            _ = Task.Run(() => ProcessItem(currentItem));
        }

        return Task.WhenAll(
            currentBatch.Select(x => x.TaskCompletionSource.Task)
        );
    }
}