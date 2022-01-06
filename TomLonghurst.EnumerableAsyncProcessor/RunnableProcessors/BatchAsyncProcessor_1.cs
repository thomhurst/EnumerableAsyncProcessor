using System.Collections.Immutable;
using TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class BatchAsyncProcessor<TSource> : AbstractAsyncProcessor<TSource>
{
    private readonly int _batchSize;

    internal BatchAsyncProcessor(int batchSize, ImmutableList<TSource> items, Func<TSource, Task> taskSelector,
        CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
        _batchSize = batchSize;
    }

    internal override async Task Process()
    {
        var batchedItems = ItemisedTaskCompletionSourceContainers.Chunk(_batchSize).ToArray();

        foreach (var currentBatch in batchedItems)
        {
            await ProcessBatch(currentBatch);
        }
    }

    private Task ProcessBatch(Tuple<TSource, TaskCompletionSource>[] currentBatch)
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