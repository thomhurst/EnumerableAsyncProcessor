using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors.Abstract;

public abstract class AbstractAsyncProcessor<TInput> : AbstractAsyncProcessorBase
{
    protected readonly IReadOnlyList<ItemTaskWrapper<TInput>> TaskWrappers;

    private readonly TaskCompletionSource[] _taskCompletionSources;

    protected override IReadOnlyList<TaskCompletionSource> EnumerableTaskCompletionSources => _taskCompletionSources;

    protected AbstractAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(cancellationTokenSource)
    {
        ValidationHelper.ThrowIfNull(items);
        ValidationHelper.ThrowIfNull(taskSelector);

        // Materialize once so one-shot or side-effecting enumerables are only enumerated a single time
        TaskWrappers = items
            .Select(item => new ItemTaskWrapper<TInput>(item, taskSelector, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)))
            .ToArray();

        _taskCompletionSources = TaskWrappers.Select(x => x.TaskCompletionSource).ToArray();
    }
}
