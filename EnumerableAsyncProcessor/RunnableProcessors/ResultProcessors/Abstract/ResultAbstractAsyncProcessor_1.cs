using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

public abstract class ResultAbstractAsyncProcessor<TOutput> : ResultAbstractAsyncProcessorBase<TOutput>
{
    private protected readonly ActionTaskWrapper<TOutput>[] TaskWrappers;

    private readonly TaskCompletionSource<TOutput>[] _taskCompletionSources;

    private protected override IReadOnlyList<TaskCompletionSource<TOutput>> EnumerableTaskCompletionSources => _taskCompletionSources;

    private protected ResultAbstractAsyncProcessor(int count, Func<Task<TOutput>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(cancellationTokenSource)
    {
        ValidationHelper.ThrowIfNegative(count);
        ValidationHelper.ThrowIfNull(taskSelector);

        TaskWrappers = Enumerable.Range(0, count)
            .Select(_ => new ActionTaskWrapper<TOutput>(taskSelector, new TaskCompletionSource<TOutput>(TaskCreationOptions.RunContinuationsAsynchronously)))
            .ToArray();

        _taskCompletionSources = TaskWrappers.Select(x => x.TaskCompletionSource).ToArray();
    }
}
