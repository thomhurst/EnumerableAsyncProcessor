using EnumerableAsyncProcessor.RunnableProcessors.Abstract;

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
        var batchedTaskWrappers = TaskWrappers.Chunk(_batchSize);
        
        foreach (var taskWrappers in batchedTaskWrappers)
        {
            await ProcessBatch(taskWrappers);
        }
    }

    private Task ProcessBatch(ActionTaskWrapper[] taskWrappers)
    {
        foreach (var taskWrapper in taskWrappers)
        {
            _ = Task.Run(() => ProcessItem(taskWrapper));
        }

        return Task.WhenAll(taskWrappers.Select(x => x.TaskCompletionSource.Task));
    }
}