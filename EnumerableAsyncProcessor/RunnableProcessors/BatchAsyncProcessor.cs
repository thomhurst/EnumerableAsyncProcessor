using EnumerableAsyncProcessor.RunnableProcessors.Abstract;
using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class BatchAsyncProcessor : AbstractAsyncProcessor
{
    private readonly int _batchSize;

    internal BatchAsyncProcessor(int batchSize, int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
        ValidationHelper.ValidateBatchSize(batchSize);

        _batchSize = batchSize;
    }
    
    internal override async Task Process()
    {
        var batchedTaskWrappers = TaskWrappers.Chunk(_batchSize);
        
        foreach (var taskWrappers in batchedTaskWrappers)
        {
            await ProcessBatch(taskWrappers).ConfigureAwait(false);
        }
    }

    private async Task ProcessBatch(ActionTaskWrapper[] taskWrappers)
    {
        await Task.WhenAll(taskWrappers.Select(tw => tw.Process(CancellationToken))).ConfigureAwait(false);
    }
}