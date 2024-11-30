using System.Collections.Immutable;
using EnumerableAsyncProcessor.RunnableProcessors.Abstract;
#if NETSTANDARD2_0
using MoreLinq;
#endif

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

    private Task ProcessBatch(Tuple<TInput, TaskCompletionSource>[] currentBatch)
    {
        foreach (var currentItem in currentBatch)
        {
            _ = Task.Run(() => ProcessItem(currentItem));
        }

        return Task.WhenAll(currentBatch.Select(x =>
        {
            var (_, taskCompletionSource) = x;
            return taskCompletionSource.Task;
        }));
    }
}