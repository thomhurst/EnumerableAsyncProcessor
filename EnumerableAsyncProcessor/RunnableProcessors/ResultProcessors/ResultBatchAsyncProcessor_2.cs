using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;
#if NETSTANDARD2_0
using MoreLinq;
#endif

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultBatchAsyncProcessor<TInput, TOutput> : ResultAbstractAsyncProcessor<TInput, TOutput>
{
    private readonly int _batchSize;

    internal ResultBatchAsyncProcessor(int batchSize, IReadOnlyCollection<TInput> items, Func<TInput, Task<TOutput>> taskSelector,
        CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
        _batchSize = batchSize;
    }

    internal override async Task Process()
    {

#if NETSTANDARD2_0
        var batchedItems = ItemisedTaskCompletionSourceContainers.Batch(_batchSize);
#else
        var batchedItems = ItemisedTaskCompletionSourceContainers.Chunk(_batchSize).ToArray();
#endif
        
        foreach (var currentBatch in batchedItems)
        {
            await ProcessBatch(currentBatch);
        }
    }

    private Task ProcessBatch(Tuple<TInput, TaskCompletionSource<TOutput>>[] currentBatch)
    {
        foreach (var currentItem in currentBatch)
        {
            _ = ProcessItem(currentItem);
        }

        return Task.WhenAll(currentBatch.Select(x =>
        {
            var (_, taskCompletionSource) = x;
            return taskCompletionSource.Task;
        }));
    }
}