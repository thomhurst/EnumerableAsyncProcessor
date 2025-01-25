using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace EnumerableAsyncProcessor.RunnableProcessors.Abstract;

public abstract class AbstractAsyncProcessor<TInput> : AbstractAsyncProcessorBase
{
    private readonly ConcurrentDictionary<int, TaskCompletionSource> _taskCompletionSources = [];
    
    protected readonly IEnumerable<ItemTaskWrapper<TInput>> TaskWrappers;

    [field: AllowNull, MaybeNull]
    protected override IEnumerable<TaskCompletionSource> EnumerableTaskCompletionSources
        => field ??= TaskWrappers.Select(x => x.TaskCompletionSource);
    
    protected AbstractAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(cancellationTokenSource)
    {
        TaskWrappers = items.Select((item, index) => new ItemTaskWrapper<TInput>(item, taskSelector, _taskCompletionSources.GetOrAdd(index, new TaskCompletionSource())));
    }
}