using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

public abstract class ResultAbstractAsyncProcessor<TInput, TOutput> : ResultAbstractAsyncProcessorBase<TOutput>
{
    private readonly ConcurrentDictionary<int, TaskCompletionSource<TOutput>> _taskCompletionSources = [];

    protected readonly IEnumerable<ItemTaskWrapper<TInput, TOutput>> TaskWrappers;
    
    protected ResultAbstractAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task<TOutput>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(cancellationTokenSource)
    {
        TaskWrappers = items.Select((item, index) => new ItemTaskWrapper<TInput, TOutput>(item, taskSelector, _taskCompletionSources.GetOrAdd(index, new TaskCompletionSource<TOutput>())));
    }

    [field: AllowNull, MaybeNull]
    protected override IEnumerable<TaskCompletionSource<TOutput>> EnumerableTaskCompletionSources =>
        field ??= TaskWrappers.Select(x => x.TaskCompletionSource);
}