using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

#if NETSTANDARD2_0
using MoreLinq;
#endif

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultBatchAsyncProcessor<TOutput> : ResultAbstractAsyncProcessor<TOutput>
{
    private readonly int _batchSize;

    internal ResultBatchAsyncProcessor(int batchSize, int count, Func<Task<TOutput>> taskSelector,
        CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
        _batchSize = batchSize;
    }

    internal override async Task Process()
    {
        
#if NETSTANDARD2_0
        var batchedTaskCompletionSources = EnumerableTaskCompletionSources.Batch(_batchSize).ToArray();
#else
        var batchedTaskCompletionSources = EnumerableTaskCompletionSources.Chunk(_batchSize).ToArray();
#endif
        
        foreach (var currentTaskCompletionSourceBatch in batchedTaskCompletionSources)
        {
            await ProcessBatch(currentTaskCompletionSourceBatch);
        }
    }

    private Task ProcessBatch(TaskCompletionSource<TOutput>[] currentTaskCompletionSourceBatch)
    {
        foreach (var taskCompletionSource in currentTaskCompletionSourceBatch)
        {
            _ = ProcessItem(taskCompletionSource);
        }

        return Task.WhenAll(currentTaskCompletionSourceBatch.Select(x => x.Task));
    }
}