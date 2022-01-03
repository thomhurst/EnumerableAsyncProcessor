using System.Threading.Channels;
using TomLonghurst.EnumerableAsyncProcessor.Interfaces;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public class ParallelProcessor<TResult> : IRunnableAsyncRegulator<TResult>
{
    private readonly List<Task<Task<TResult>>> _initialTasks;
    private readonly Task _totalProgressTask;

    public ParallelProcessor(List<Task<Task<TResult>>> initialTasks)
    {
        _initialTasks = initialTasks;

        _totalProgressTask = Parallel.ForEachAsync(_initialTasks,
            new ParallelOptions { MaxDegreeOfParallelism = -1 },
            async (task, token) =>
            {
                task.Start();
                await task.Unwrap();
            });
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
        return _totalProgressTask;
    }
}