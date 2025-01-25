using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace EnumerableAsyncProcessor.RunnableProcessors.Abstract;

public abstract class AbstractAsyncProcessor : AbstractAsyncProcessorBase
{
    private readonly ConcurrentDictionary<int, TaskCompletionSource> _taskCompletionSources = [];

    protected readonly IEnumerable<ActionTaskWrapper> TaskWrappers;

    [field: AllowNull, MaybeNull]
    protected override IEnumerable<TaskCompletionSource> EnumerableTaskCompletionSources
        => field ??= TaskWrappers.Select(x => x.TaskCompletionSource);

    
    protected AbstractAsyncProcessor(int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(cancellationTokenSource)
    {
        TaskWrappers = Enumerable.Range(0, count).Select(index => new ActionTaskWrapper(taskSelector, _taskCompletionSources.GetOrAdd(index, new TaskCompletionSource())));
    }
}