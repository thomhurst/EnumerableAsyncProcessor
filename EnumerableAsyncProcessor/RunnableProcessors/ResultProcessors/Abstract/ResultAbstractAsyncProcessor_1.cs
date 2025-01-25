using System.Diagnostics.CodeAnalysis;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

public abstract class ResultAbstractAsyncProcessor<TOutput> : ResultAbstractAsyncProcessorBase<TOutput>
{
    protected readonly IEnumerable<ActionTaskWrapper<TOutput>> TaskWrappers;

    protected ResultAbstractAsyncProcessor(int count, Func<Task<TOutput>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(cancellationTokenSource)
    {
        TaskWrappers = Enumerable.Range(0, count).Select(_ => new ActionTaskWrapper<TOutput>(taskSelector));
    }

    [field: AllowNull, MaybeNull]
    protected override IEnumerable<TaskCompletionSource<TOutput>> EnumerableTaskCompletionSources =>
        field ??= TaskWrappers.Select(x => x.TaskCompletionSource);

    protected Task ProcessItem(ActionTaskWrapper<TOutput> taskWrapper)
    {
        return taskWrapper.Process(CancellationToken);
    }
}