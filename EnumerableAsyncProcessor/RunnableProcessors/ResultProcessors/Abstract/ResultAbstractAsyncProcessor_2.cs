using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

public abstract class ResultAbstractAsyncProcessor<TInput, TOutput> : ResultAbstractAsyncProcessorBase<TOutput>
{
    protected readonly IReadOnlyList<ItemTaskWrapper<TInput, TOutput>> TaskWrappers;

    private readonly TaskCompletionSource<TOutput>[] _taskCompletionSources;

    protected override IReadOnlyList<TaskCompletionSource<TOutput>> EnumerableTaskCompletionSources => _taskCompletionSources;

    protected ResultAbstractAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task<TOutput>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(cancellationTokenSource)
    {
        ValidationHelper.ThrowIfNull(items);
        ValidationHelper.ThrowIfNull(taskSelector);

        // Materialize once so one-shot or side-effecting enumerables are only enumerated a single time
        TaskWrappers = items
            .Select(item => new ItemTaskWrapper<TInput, TOutput>(item, taskSelector, new TaskCompletionSource<TOutput>(TaskCreationOptions.RunContinuationsAsynchronously)))
            .ToArray();

        _taskCompletionSources = TaskWrappers.Select(x => x.TaskCompletionSource).ToArray();
    }
}
