using TomLonghurst.EnumerableAsyncProcessor.Interfaces;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class OneAtATimeAsyncProcessor<TResult> : IRunnableAsyncRegulator<TResult>
{
    private readonly List<Task<Task<TResult>>> _initialTasks;
    private readonly TaskCompletionSource _taskCompletionSource = new();

    public OneAtATimeAsyncProcessor(List<Task<Task<TResult>>> initialTasks)
    {
        _initialTasks = initialTasks;

        _ = Process();
    }

    private async Task Process()
    {
        try
        {
            foreach (var task in _initialTasks)
            {
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