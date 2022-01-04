using TomLonghurst.EnumerableAsyncProcessor.Helpers;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class BatchAsyncProcessor<TResult> : AbstractAsyncProcessor<TResult>
{
    private readonly int _batchSize;
    private readonly TaskCompletionSource _taskCompletionSource = new();

    public BatchAsyncProcessor(List<Task<Task<TResult>>> initialTasks, int batchSize,
        CancellationToken cancellationToken) : base(initialTasks, cancellationToken)
    {
        _batchSize = batchSize;
    }

    internal override async Task Process()
    {
        try
        {
            var batchedTasks = InitialTasks.Chunk(_batchSize);

            foreach (var currentBatch in batchedTasks)
            {
                CancellationToken.ThrowIfCancellationRequested();
                TaskHelper.StartAll(currentBatch);
                await Task.WhenAll(currentBatch);
            }

            _taskCompletionSource.TrySetResult();
        }
        catch (Exception e)
        {
            _taskCompletionSource.TrySetException(e);
        }
    }

    public override Task ContinuationTask => _taskCompletionSource.Task;
}

public class BatchAsyncProcessor : AbstractAsyncProcessor
{
    private readonly int _batchSize;
    private readonly TaskCompletionSource _taskCompletionSource = new();

    public BatchAsyncProcessor(List<Task<Task>> initialTasks, int batchSize,
        CancellationToken cancellationToken) : base(initialTasks, cancellationToken)
    {
        _batchSize = batchSize;
    }

    internal override async Task Process()
    {
        try
        {
            var batchedTasks = InitialTasks.Chunk(_batchSize);

            foreach (var currentBatch in batchedTasks)
            {
                CancellationToken.ThrowIfCancellationRequested();
                TaskHelper.StartAll(currentBatch);
                await Task.WhenAll(currentBatch);
            }

            _taskCompletionSource.TrySetResult();
        }
        catch (Exception e)
        {
            _taskCompletionSource.TrySetException(e);
        }
    }

    public override Task Task => _taskCompletionSource.Task;
}