using TomLonghurst.EnumerableAsyncProcessor.Interfaces;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class RateLimitedParallelAsyncProcessor<TResult> : IRunnableAsyncRegulator<TResult>
{
    private readonly List<Task<Task<TResult>>> _initialTasks;
    private readonly Task _totalProgressTask;

    public RateLimitedParallelAsyncProcessor(List<Task<Task<TResult>>> initialTasks, int levelsOfParallelism)
    {
        _initialTasks = initialTasks;

        _totalProgressTask = Parallel.ForEachAsync(_initialTasks,
            new ParallelOptions { MaxDegreeOfParallelism = levelsOfParallelism },
            async (task, token) =>
            {
                task.Start();
                await task.Unwrap();
            });
    }

    public IEnumerable<Task<TResult>> GetEnumerableTasks()
    {
        return _initialTasks.Select(x => x.Unwrap());
    }

    public async Task<IEnumerable<TResult>> GetResults()
    {
        return await Task.WhenAll(GetEnumerableTasks());
    }

    public Task GetTotalProgressTask()
    {
        return _totalProgressTask;
    }
}