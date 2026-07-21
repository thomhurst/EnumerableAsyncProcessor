using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors.Abstract;

public abstract class AbstractAsyncProcessor : AbstractAsyncProcessorBase
{
    protected readonly IReadOnlyList<ActionTaskWrapper> TaskWrappers;

    private readonly TaskCompletionSource[] _taskCompletionSources;

    protected override IReadOnlyList<TaskCompletionSource> EnumerableTaskCompletionSources => _taskCompletionSources;

    protected AbstractAsyncProcessor(int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(cancellationTokenSource)
    {
        ValidationHelper.ValidateCount(count);
        ValidationHelper.ThrowIfNull(taskSelector);

        var taskWrappers = new ActionTaskWrapper[count];
        _taskCompletionSources = new TaskCompletionSource[count];

        for (var i = 0; i < count; i++)
        {
            var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _taskCompletionSources[i] = taskCompletionSource;
            taskWrappers[i] = new ActionTaskWrapper(taskSelector, taskCompletionSource);
        }

        TaskWrappers = taskWrappers;
    }
}
