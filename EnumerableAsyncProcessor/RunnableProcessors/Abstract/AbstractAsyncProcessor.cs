using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors.Abstract;

public abstract class AbstractAsyncProcessor : AbstractAsyncProcessorBase
{
    private protected readonly ActionTaskWrapper[] TaskWrappers;

    private readonly TaskCompletionSource[] _taskCompletionSources;

    private protected override IReadOnlyList<TaskCompletionSource> EnumerableTaskCompletionSources => _taskCompletionSources;

    private protected AbstractAsyncProcessor(int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(cancellationTokenSource)
    {
        ValidationHelper.ThrowIfNegative(count);
        ValidationHelper.ThrowIfNull(taskSelector);

        TaskWrappers = Enumerable.Range(0, count)
            .Select(_ => new ActionTaskWrapper(taskSelector, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)))
            .ToArray();

        _taskCompletionSources = TaskWrappers.Select(x => x.TaskCompletionSource).ToArray();
    }
}
