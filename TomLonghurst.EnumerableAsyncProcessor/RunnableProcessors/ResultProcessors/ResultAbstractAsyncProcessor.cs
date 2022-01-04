using TomLonghurst.EnumerableAsyncProcessor.Interfaces;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public abstract class ResultAbstractAsyncProcessor<TSource, TResult> : ResultAbstractAsyncProcessor_Base<TResult>
{
    protected readonly IEnumerable<ItemisedTaskCompletionSourceContainer<TSource, TResult>> ItemisedTaskCompletionSourceContainers;
    protected readonly Func<TSource, Task<TResult>> TaskSelector;

    protected ResultAbstractAsyncProcessor(IReadOnlyCollection<TSource> items, Func<TSource, Task<TResult>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items.Count, cancellationTokenSource)
    {
        ItemisedTaskCompletionSourceContainers = items.Select((item, index) =>
            new ItemisedTaskCompletionSourceContainer<TSource, TResult>(item, EnumerableTaskCompletionSources[index]));
        TaskSelector = taskSelector;
    }
}

public abstract class ResultAbstractAsyncProcessor<TResult> : ResultAbstractAsyncProcessor_Base<TResult>
{
    protected readonly Func<Task<TResult>> TaskSelector;

    protected ResultAbstractAsyncProcessor(int count, Func<Task<TResult>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, cancellationTokenSource)
    {
        TaskSelector = taskSelector;
    }
}

public abstract class ResultAbstractAsyncProcessor_Base<TResult> : IAsyncProcessor<TResult>, IDisposable
{
    protected readonly List<TaskCompletionSource<TResult>> EnumerableTaskCompletionSources;
    protected readonly List<Task<TResult>> EnumerableTasks;
    protected readonly CancellationToken CancellationToken;
    
    private readonly CancellationTokenSource _cancellationTokenSource;


    protected ResultAbstractAsyncProcessor_Base(int count, CancellationTokenSource cancellationTokenSource)
    {
        EnumerableTaskCompletionSources = Enumerable.Range(0, count).Select(_ => new TaskCompletionSource<TResult>()).ToList();
        EnumerableTasks = EnumerableTaskCompletionSources.Select(x => x.Task).ToList();
        ContinuationTask = Task.WhenAll(EnumerableTasks);
        
        _cancellationTokenSource = cancellationTokenSource;
        CancellationToken = cancellationTokenSource.Token;

        cancellationTokenSource.Token.Register(Dispose);
        cancellationTokenSource.Token.ThrowIfCancellationRequested();
    }

    internal abstract Task Process();
    
    public IEnumerable<Task<TResult>> GetEnumerableTasks()
    {
        return EnumerableTasks;
    }

    public async Task<IEnumerable<TResult>> GetResults()
    {
        return await Task.WhenAll(GetEnumerableTasks());
    }

    public Task ContinuationTask { get; }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        EnumerableTaskCompletionSources.ForEach(t => t.TrySetCanceled(CancellationToken));
        _cancellationTokenSource.Dispose();
    }
}