﻿using System.Collections.Immutable;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultBatchAsyncProcessor<TSource, TResult> : ResultAbstractAsyncProcessor<TSource, TResult>
{
    private readonly int _batchSize;

    internal ResultBatchAsyncProcessor(int batchSize, IReadOnlyCollection<TSource> items, Func<TSource, Task<TResult>> taskSelector,
        CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
        _batchSize = batchSize;
    }

    internal override async Task Process()
    {
        var batchedItems = ItemisedTaskCompletionSourceContainers.Chunk(_batchSize).ToArray();

        foreach (var currentItemBatch in batchedItems)
        {
            await ProcessBatch(currentItemBatch);
        }
    }

    private Task ProcessBatch(ItemisedTaskCompletionSourceContainer<TSource, TResult>[] currentItemBatch)
    {
        foreach (var currentItem in currentItemBatch)
        {
            ProcessItem(currentItem);
        }

        return Task.WhenAll(currentItemBatch.Select(x => x.TaskCompletionSource.Task));
    }

    private async Task ProcessItem(ItemisedTaskCompletionSourceContainer<TSource, TResult> currentItem)
    {
        try
        {
            var result = await TaskSelector(currentItem.Item);
            currentItem.TaskCompletionSource.SetResult(result);
        }
        catch (Exception e)
        {
            currentItem.TaskCompletionSource.SetException(e);
        }
    }
}

public class ResultBatchAsyncProcessor<TResult> : ResultAbstractAsyncProcessor<TResult>
{
    private readonly int _batchSize;

    internal ResultBatchAsyncProcessor(int batchSize, int count, Func<Task<TResult>> taskSelector,
        CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
        _batchSize = batchSize;
    }

    internal override async Task Process()
    {
        var batchedTaskCompletionSources = EnumerableTaskCompletionSources.Chunk(_batchSize).ToArray();

        for (var i = 0; i < batchedTaskCompletionSources.Length; i++)
        {
            var currentTaskCompletionSourceBatch = batchedTaskCompletionSources[i];

            await ProcessBatch(currentTaskCompletionSourceBatch);
        }
    }

    private Task ProcessBatch(TaskCompletionSource<TResult>[] currentTaskCompletionSourceBatch)
    {
        foreach (var taskCompletionSource in currentTaskCompletionSourceBatch)
        {
            ProcessItem(taskCompletionSource);
        }

        return Task.WhenAll(currentTaskCompletionSourceBatch.Select(x => x.Task));
    }

    private async Task ProcessItem(TaskCompletionSource<TResult> taskCompletionSource)
    {
        try
        {
            var result = await TaskSelector();
            taskCompletionSource.SetResult(result);
        }
        catch (Exception e)
        {
            taskCompletionSource.SetException(e);
        }
    }
}