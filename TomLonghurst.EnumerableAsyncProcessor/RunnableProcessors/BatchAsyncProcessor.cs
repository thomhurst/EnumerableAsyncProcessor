using System.Collections.Immutable;

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

    private Task ProcessBatch(ItemisedTaskCompletionSourceContainer<TSource>[] currentBatch)
    {
        foreach (var currentItem in currentBatch)
        {
            _ = ProcessItem(currentItem);
        }

        return Task.WhenAll(currentBatch.Select(x => x.TaskCompletionSource.Task));
    }

    private async Task ProcessItem(ItemisedTaskCompletionSourceContainer<TSource> currentItem)
    {
        try
        {
            await TaskSelector(currentItem.Item);
            currentItem.TaskCompletionSource.SetResult();
        }
        catch (Exception e)
        {
            currentItem.TaskCompletionSource.SetException(e);
        }
    }
}

public class BatchAsyncProcessor : AbstractAsyncProcessor
{
    private readonly int _batchSize;

    internal BatchAsyncProcessor(int batchSize, int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
        _batchSize = batchSize;
    }

    internal override async Task Process()
    {
        var batchedTaskCompletionSources = EnumerableTaskCompletionSources.Chunk(_batchSize).ToArray();

        foreach (var currentTaskCompletionSourceBatch in batchedTaskCompletionSources)
        {
            await ProcessBatch(currentTaskCompletionSourceBatch);
        }
    }

    private Task ProcessBatch(TaskCompletionSource[] currentTaskCompletionSourceBatch)
    {
        foreach (var taskCompletionSource in currentTaskCompletionSourceBatch)
        {
            _ = ProcessItem(taskCompletionSource);
        }

        return Task.WhenAll(currentTaskCompletionSourceBatch.Select(x => x.Task));
    }

    private async Task ProcessItem(TaskCompletionSource taskCompletionSource)
    {
        try
        {
            await TaskSelector();
            taskCompletionSource.SetResult();
        }
        catch (Exception e)
        {
            taskCompletionSource.SetException(e);
        }
    }
}