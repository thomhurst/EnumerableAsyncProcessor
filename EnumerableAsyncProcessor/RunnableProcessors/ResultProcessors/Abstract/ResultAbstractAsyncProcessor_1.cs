using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

public abstract class ResultAbstractAsyncProcessor<TOutput> : ResultAbstractAsyncProcessorBase<TOutput>
{
    private readonly ConcurrentDictionary<int, TaskCompletionSource<TOutput>> _taskCompletionSources = [];

    protected readonly IEnumerable<ActionTaskWrapper<TOutput>> TaskWrappers;

    protected ResultAbstractAsyncProcessor(int count, Func<Task<TOutput>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(cancellationTokenSource)
    {
        TaskWrappers = Enumerable.Range(0, count).Select(index => new ActionTaskWrapper<TOutput>(taskSelector, _taskCompletionSources.GetOrAdd(index, new TaskCompletionSource<TOutput>())));
    }

    [field: AllowNull, MaybeNull]
    protected override IEnumerable<TaskCompletionSource<TOutput>> EnumerableTaskCompletionSources =>
        field ??= TaskWrappers.Select(x => x.TaskCompletionSource);
}