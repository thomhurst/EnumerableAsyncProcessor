using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

public abstract class ResultAbstractAsyncProcessor<TOutput> : ResultAbstractAsyncProcessorBase<TOutput>
{
    protected readonly IReadOnlyList<ActionTaskWrapper<TOutput>> TaskWrappers;

    private readonly TaskCompletionSource<TOutput>[] _taskCompletionSources;

    protected override IReadOnlyList<TaskCompletionSource<TOutput>> EnumerableTaskCompletionSources => _taskCompletionSources;

    protected ResultAbstractAsyncProcessor(int count, Func<Task<TOutput>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(cancellationTokenSource)
    {
        ValidationHelper.ValidateCount(count);
        ValidationHelper.ThrowIfNull(taskSelector);

        var taskWrappers = new ActionTaskWrapper<TOutput>[count];
        _taskCompletionSources = new TaskCompletionSource<TOutput>[count];

        for (var i = 0; i < count; i++)
        {
            var taskCompletionSource = new TaskCompletionSource<TOutput>(TaskCreationOptions.RunContinuationsAsynchronously);
            _taskCompletionSources[i] = taskCompletionSource;
            taskWrappers[i] = new ActionTaskWrapper<TOutput>(taskSelector, taskCompletionSource);
        }

        TaskWrappers = taskWrappers;
    }
}
