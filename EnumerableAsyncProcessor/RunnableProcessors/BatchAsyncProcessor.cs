using EnumerableAsyncProcessor.RunnableProcessors.Abstract;
#if NETSTANDARD2_0
using MoreLinq;
#endif

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class BatchAsyncProcessor : AbstractAsyncProcessor
{
    private readonly int _batchSize;

    internal BatchAsyncProcessor(int batchSize, int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
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

    private Task ProcessBatch(TaskCompletionSource[] currentTaskCompletionSourceBatch)
    {
        foreach (var taskCompletionSource in currentTaskCompletionSourceBatch)
        {
            _ = Task.Run(() => ProcessItem(taskCompletionSource));
        }

        return Task.WhenAll(currentTaskCompletionSourceBatch.Select(x => x.Task));
    }
}