using System.Collections.Immutable;
using EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class BatchAsyncProcessor<TInput> : AbstractAsyncProcessor<TInput>
{
    private readonly int _batchSize;

    internal BatchAsyncProcessor(int batchSize, ImmutableList<TInput> items, Func<TInput, Task> taskSelector,
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

    private Task ProcessBatch(Tuple<TInput, TaskCompletionSource>[] currentBatch)
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