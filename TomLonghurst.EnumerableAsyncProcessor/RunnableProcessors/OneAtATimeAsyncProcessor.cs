using TomLonghurst.EnumerableAsyncProcessor.Interfaces;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class OneAtATimeAsyncProcessor<TResult> : IRunnableAsyncRegulator<TResult>
{
    private readonly List<Task<Task<TResult>>> _initialTasks;
    private readonly CancellationToken _cancellationToken;
    private readonly TaskCompletionSource _taskCompletionSource = new();

    public OneAtATimeAsyncProcessor(List<Task<Task<TResult>>> initialTasks, CancellationToken cancellationToken)
    {
        _initialTasks = initialTasks;
        _cancellationToken = cancellationToken;

        _ = Process();
    }

    private async Task Process()
    {
        try
        {
            foreach (var task in _initialTasks)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                task.Start();
                await task.Unwrap();
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