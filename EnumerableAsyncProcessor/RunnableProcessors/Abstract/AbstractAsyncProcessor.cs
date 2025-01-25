using System.Diagnostics.CodeAnalysis;

namespace EnumerableAsyncProcessor.RunnableProcessors.Abstract;

public abstract class AbstractAsyncProcessor : AbstractAsyncProcessorBase
{
    protected readonly IEnumerable<ActionTaskWrapper> TaskWrappers;

    [field: AllowNull, MaybeNull]
    protected override IEnumerable<TaskCompletionSource> EnumerableTaskCompletionSources
        => field ??= TaskWrappers.Select(x => x.TaskCompletionSource);

    
    protected AbstractAsyncProcessor(int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(cancellationTokenSource)
    {
        TaskWrappers = Enumerable.Range(0, count).Select(_ => new ActionTaskWrapper(taskSelector));
    }
}