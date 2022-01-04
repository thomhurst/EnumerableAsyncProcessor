using System.Collections.Immutable;
using TomLonghurst.EnumerableAsyncProcessor.Interfaces;

namespace TomLonghurst.EnumerableAsyncProcessor.RunnableProcessors;

public abstract class AbstractAsyncProcessor : AbstractAsyncProcessor_Base
{
    protected readonly Func<Task> TaskSelector;

    protected AbstractAsyncProcessor(int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, cancellationTokenSource)
    {
        TaskSelector = taskSelector;
    }
}

public abstract class AbstractAsyncProcessor<TSource> : AbstractAsyncProcessor_Base
{
    protected readonly IEnumerable<ItemisedTaskCompletionSourceContainer<TSource>> ItemisedTaskCompletionSourceContainers;
    protected readonly Func<TSource, Task> TaskSelector;

    protected AbstractAsyncProcessor(ImmutableList<TSource> items, Func<TSource, Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items.Count, cancellationTokenSource)
    {
        ItemisedTaskCompletionSourceContainers = items.Select((item, index) =>
            new ItemisedTaskCompletionSourceContainer<TSource>(item, EnumerableTaskCompletionSources[index]));
        TaskSelector = taskSelector;
    }
}

public abstract class AbstractAsyncProcessor_Base : IAsyncProcessor, IDisposable
{
    protected readonly List<TaskCompletionSource> EnumerableTaskCompletionSources;
    protected readonly List<Task> EnumerableTasks;
    protected readonly CancellationToken CancellationToken;
    
    private readonly CancellationTokenSource _cancellationTokenSource;


    protected AbstractAsyncProcessor_Base(int count, CancellationTokenSource cancellationTokenSource)
    {
        EnumerableTaskCompletionSources = Enumerable.Range(0, count).Select(x => new TaskCompletionSource()).ToList();
        EnumerableTasks = EnumerableTaskCompletionSources.Select(x => x.Task).ToList();
        Task = Task.WhenAll(EnumerableTasks);
        
        _cancellationTokenSource = cancellationTokenSource;
        CancellationToken = cancellationTokenSource.Token;

        cancellationTokenSource.Token.Register(Dispose);
        cancellationTokenSource.Token.ThrowIfCancellationRequested();
    }

    internal abstract Task Process();
    
    public IEnumerable<Task> GetEnumerableTasks()
    {
        return EnumerableTasks;
    }
    
    public Task Task { get; }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        EnumerableTaskCompletionSources.ForEach(t => t.TrySetCanceled(CancellationToken));
        _cancellationTokenSource.Dispose();
    }
}