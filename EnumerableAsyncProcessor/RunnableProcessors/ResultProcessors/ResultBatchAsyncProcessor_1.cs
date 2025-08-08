using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;
using EnumerableAsyncProcessor.Validation;


namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultBatchAsyncProcessor<TOutput> : ResultAbstractAsyncProcessor<TOutput>
{
    private readonly int _batchSize;

    internal ResultBatchAsyncProcessor(int batchSize, int count, Func<Task<TOutput>> taskSelector,
        CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
        ValidationHelper.ValidateBatchSize(batchSize);

        _batchSize = batchSize;
    }

    internal override async Task Process()
    {
        var batchedTaskCompletionSources = TaskWrappers.Chunk(_batchSize);
        
        foreach (var currentTaskCompletionSourceBatch in batchedTaskCompletionSources)
        {
            await ProcessBatch(currentTaskCompletionSourceBatch).ConfigureAwait(false);
        }
    }

    private Task ProcessBatch(ActionTaskWrapper<TOutput>[] batch)
    {
        return Task.WhenAll(batch.Select(tw => tw.Process(CancellationToken)));
    }
}