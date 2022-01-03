using TomLonghurst.EnumerableAsyncProcessor.Helpers;
using TomLonghurst.EnumerableAsyncProcessor.Interfaces;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class AsyncBatchProcessor<TResult> : IRunnableAsyncRegulator<TResult>
{
    private readonly List<Task<Task<TResult>>> _initialTasks;
    private readonly int _batchSize;
    private readonly TaskCompletionSource _taskCompletionSource = new();

    public AsyncBatchProcessor(List<Task<Task<TResult>>> initialTasks, int batchSize)
    {
        _initialTasks = initialTasks;
        _batchSize = batchSize;

        _ = Process();
    }

    private async Task Process()
    {
        try
        {
            var batchedTasks = _initialTasks.Chunk(_batchSize);

            foreach (var currentBatch in batchedTasks)
            {
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

    public IEnumerable<Task<TResult>> GetInnerTasks()
    {
        return _initialTasks.Select(x => x.Unwrap());
    }

    public async Task<IEnumerable<TResult>> GetResults()
    {
        return await Task.WhenAll(GetInnerTasks());
    }

    public Task GetTotalProgressTask()
    {
        return _taskCompletionSource.Task;
    }
}