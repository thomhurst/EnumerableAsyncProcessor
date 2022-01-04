using TomLonghurst.EnumerableAsyncProcessor.Interfaces;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public abstract class AbstractAsyncProcessor<TResult> : IAsyncProcessor<TResult>
{
    protected readonly List<Task<Task<TResult>>> _initialTasks;
    protected readonly CancellationToken _cancellationToken;

    protected AbstractAsyncProcessor(List<Task<Task<TResult>>> initialTasks, CancellationToken cancellationToken)
    {
        _initialTasks = initialTasks;
        _cancellationToken = cancellationToken;

        cancellationToken.Register(() => initialTasks.ForEach(x => x.Dispose()));
    }
        
    internal abstract Task Process();
    
    public IEnumerable<Task<TResult>> GetEnumerableTasks()
    {
        return _initialTasks.Select(x => x.Unwrap());
    }

    public async Task<IEnumerable<TResult>> GetResults()
    {
        return await Task.WhenAll(GetEnumerableTasks());
    }

    public abstract Task GetOverallProgressTask();
}