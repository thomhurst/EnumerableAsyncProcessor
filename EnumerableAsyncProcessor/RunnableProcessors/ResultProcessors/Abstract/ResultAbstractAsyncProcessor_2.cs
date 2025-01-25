using System.Diagnostics.CodeAnalysis;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

public abstract class ResultAbstractAsyncProcessor<TInput, TOutput> : ResultAbstractAsyncProcessorBase<TOutput>
{
    protected readonly IEnumerable<ItemTaskWrapper<TInput, TOutput>> TaskWrappers;
    
    protected ResultAbstractAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task<TOutput>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(cancellationTokenSource)
    {
        TaskWrappers = items.Select(item => new ItemTaskWrapper<TInput, TOutput>(item, taskSelector));
    }

    [field: AllowNull, MaybeNull]
    protected override IEnumerable<TaskCompletionSource<TOutput>> EnumerableTaskCompletionSources =>
        field ??= TaskWrappers.Select(x => x.TaskCompletionSource);
}