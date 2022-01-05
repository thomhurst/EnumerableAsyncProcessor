using System.Runtime.CompilerServices;
using TomLonghurst.EnumerableAsyncProcessor.Interfaces;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public abstract class ResultAbstractAsyncProcessor<TResult> : ResultAbstractAsyncProcessor_Base<TResult>
{
    private readonly Func<Task<TResult>> _taskSelector;

    protected ResultAbstractAsyncProcessor(int count, Func<Task<TResult>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, cancellationTokenSource)
    {
        _taskSelector = taskSelector;
    }
    
    protected async Task ProcessItem(TaskCompletionSource<TResult> taskCompletionSource)
    {
        try
        {
            CancellationToken.ThrowIfCancellationRequested();
            var result = await _taskSelector();
            taskCompletionSource.SetResult(result);
        }
        catch (Exception e)
        {
            taskCompletionSource.SetException(e);
        }
    }
}

public abstract class ResultAbstractAsyncProcessor<TSource, TResult> : ResultAbstractAsyncProcessor_Base<TResult>
{
    protected readonly IEnumerable<ItemisedTaskCompletionSourceContainer<TSource, TResult>> ItemisedTaskCompletionSourceContainers;

    private readonly Func<TSource, Task<TResult>> _taskSelector;

    protected ResultAbstractAsyncProcessor(IReadOnlyCollection<TSource> items, Func<TSource, Task<TResult>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items.Count, cancellationTokenSource)
    {
        ItemisedTaskCompletionSourceContainers = items.Select((item, index) =>
            new ItemisedTaskCompletionSourceContainer<TSource, TResult>(item, EnumerableTaskCompletionSources[index]));
        _taskSelector = taskSelector;
    }
    
    protected async Task ProcessItem(ItemisedTaskCompletionSourceContainer<TSource, TResult> itemisedTaskCompletionSourceContainer)
    {
        try
        {
            CancellationToken.ThrowIfCancellationRequested();
            var result = await _taskSelector(itemisedTaskCompletionSourceContainer.Item);
            itemisedTaskCompletionSourceContainer.TaskCompletionSource.SetResult(result);
        }
        catch (Exception e)
        {
            itemisedTaskCompletionSourceContainer.TaskCompletionSource.SetException(e);
        }
    }
}

public abstract class ResultAbstractAsyncProcessor_Base<TResult> : IAsyncProcessor<TResult>, IDisposable
{
    protected readonly List<TaskCompletionSource<TResult>> EnumerableTaskCompletionSources;
    protected readonly CancellationToken CancellationToken;

    private readonly List<Task<TResult>> _enumerableTasks;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task<TResult[]> _results;


    protected ResultAbstractAsyncProcessor_Base(int count, CancellationTokenSource cancellationTokenSource)
    {
        EnumerableTaskCompletionSources = Enumerable.Range(0, count).Select(_ => new TaskCompletionSource<TResult>()).ToList();
        _enumerableTasks = EnumerableTaskCompletionSources.Select(x => x.Task).ToList();
        _results = Task.WhenAll(_enumerableTasks);
        
        _cancellationTokenSource = cancellationTokenSource;
        CancellationToken = cancellationTokenSource.Token;

        cancellationTokenSource.Token.Register(Dispose);
        cancellationTokenSource.Token.ThrowIfCancellationRequested();
    }

    internal abstract Task Process();
    
    public IEnumerable<Task<TResult>> GetEnumerableTasks()
    {
        return _enumerableTasks;
    }

    public Task<TResult[]> GetResults()
    {
        return _results;
    }

    public TaskAwaiter<TResult[]> GetAwaiter()
    {
        return GetResults().GetAwaiter();
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        EnumerableTaskCompletionSources.ForEach(t => t.TrySetCanceled(CancellationToken));
        _cancellationTokenSource.Dispose();
        GC.SuppressFinalize(this);
    }
}