using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultBatchAsyncProcessor<TInput, TOutput> : ResultAbstractAsyncProcessor<TInput, TOutput>
{
    private readonly int _batchSize;

    internal ResultBatchAsyncProcessor(int batchSize, IEnumerable<TInput> items, Func<TInput, Task<TOutput>> taskSelector,
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

    private Task ProcessBatch(ItemTaskWrapper<TInput, TOutput>[] currentBatch)
    {
        return Task.WhenAll(currentBatch.Select(tw => tw.Process(CancellationToken)));
    }
}