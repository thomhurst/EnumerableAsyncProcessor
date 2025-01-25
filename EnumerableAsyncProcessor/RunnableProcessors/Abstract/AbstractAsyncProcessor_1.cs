using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace EnumerableAsyncProcessor.RunnableProcessors.Abstract;

public abstract class AbstractAsyncProcessor<TInput> : AbstractAsyncProcessorBase
{
    protected readonly IEnumerable<ItemTaskWrapper<TInput>> TaskWrappers;

    [field: AllowNull, MaybeNull]
    protected override IEnumerable<TaskCompletionSource> EnumerableTaskCompletionSources
        => field ??= TaskWrappers.Select(x => x.TaskCompletionSource);
    
    protected AbstractAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(cancellationTokenSource)
    {
        TaskWrappers = items.Select(item => new ItemTaskWrapper<TInput>(item, taskSelector));;
    }
    
    protected Task ProcessItem(ItemTaskWrapper<TInput> taskWrapper)
    {
        return taskWrapper.Process(CancellationToken);
    }
}