using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;


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
        var batchedTaskCompletionSources = TaskWrappers.Chunk(_batchSize);
        
        foreach (var currentTaskCompletionSourceBatch in batchedTaskCompletionSources)
        {
            await ProcessBatch(currentTaskCompletionSourceBatch);
        }
    }

    private Task ProcessBatch(ActionTaskWrapper<TOutput>[] batch)
    {
        foreach (var taskWrapper in batch)
        {
            _ = Task.Run(() => ProcessItem(taskWrapper));
        }

        return Task.WhenAll(batch.Select(x => x.TaskCompletionSource.Task));
    }
}