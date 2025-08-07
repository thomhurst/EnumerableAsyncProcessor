using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

public abstract class ResultAbstractAsyncProcessor<TInput, TOutput> : ResultAbstractAsyncProcessorBase<TOutput>
{
    private readonly ConcurrentDictionary<int, TaskCompletionSource<TOutput>> _taskCompletionSources = [];

    protected readonly IEnumerable<ItemTaskWrapper<TInput, TOutput>> TaskWrappers;
    
    protected ResultAbstractAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task<TOutput>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(cancellationTokenSource)
    {
        var isEmpty = ValidationHelper.ValidateEnumerable(items);
        ValidationHelper.ThrowIfNull(taskSelector);

        // Provide optimization for empty collections
        if (isEmpty)
        {
            TaskWrappers = [];
            return;
        }

        // Get count for performance warnings if collection implements ICollection
        if (items is ICollection<TInput> collection)
        {
            var warning = ValidationHelper.GetPerformanceWarning(collection.Count);
            if (warning != null)
            {
                // In a real application, you might want to log this warning
                // For now, we'll just store it as a comment that could be used by logging
                _ = warning;
            }
        }

        TaskWrappers = items.Select((item, index) => new ItemTaskWrapper<TInput, TOutput>(item, taskSelector, _taskCompletionSources.GetOrAdd(index, new TaskCompletionSource<TOutput>())));
    }

    [field: AllowNull, MaybeNull]
    protected override IEnumerable<TaskCompletionSource<TOutput>> EnumerableTaskCompletionSources =>
        field ??= TaskWrappers.Select(x => x.TaskCompletionSource);
}