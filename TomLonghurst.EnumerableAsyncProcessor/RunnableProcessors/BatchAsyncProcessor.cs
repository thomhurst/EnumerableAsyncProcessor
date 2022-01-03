using TomLonghurst.EnumerableAsyncProcessor.Helpers;
using TomLonghurst.EnumerableAsyncProcessor.Interfaces;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class BatchAsyncProcessor<TResult> : IRunnableAsyncRegulator<TResult>
{
    private readonly List<Task<Task<TResult>>> _initialTasks;
    private readonly int _batchSize;
    private readonly CancellationToken _cancellationToken;
    private readonly TaskCompletionSource _taskCompletionSource = new();

    public BatchAsyncProcessor(List<Task<Task<TResult>>> initialTasks, int batchSize,
        CancellationToken cancellationToken)
    {
        _initialTasks = initialTasks;
        _batchSize = batchSize;
        _cancellationToken = cancellationToken;

        _ = Process();
    }

    private async Task Process()
    {
        try
        {
            var batchedTasks = _initialTasks.Chunk(_batchSize);

            foreach (var currentBatch in batchedTasks)
            {
                _cancellationToken.ThrowIfCancellationRequested();
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

    public IEnumerable<Task<TResult>> GetEnumerableTasks()
    {
        return _initialTasks.Select(x => x.Unwrap());
    }

    public async Task<IEnumerable<TResult>> GetResults()
    {
        return await Task.WhenAll(GetEnumerableTasks());
    }

    public Task GetOverallProgressTask()
    {
        return _taskCompletionSource.Task;
    }
}