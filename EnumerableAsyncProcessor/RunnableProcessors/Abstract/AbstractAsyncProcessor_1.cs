using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors.Abstract;

public abstract class AbstractAsyncProcessor<TInput> : AbstractAsyncProcessorBase
{
    private readonly ConcurrentDictionary<int, TaskCompletionSource> _taskCompletionSources = [];
    
    protected readonly IEnumerable<ItemTaskWrapper<TInput>> TaskWrappers;

    [field: AllowNull, MaybeNull]
    protected override IEnumerable<TaskCompletionSource> EnumerableTaskCompletionSources
        => field ??= TaskWrappers.Select(x => x.TaskCompletionSource);
    
    protected AbstractAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(cancellationTokenSource)
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

        TaskWrappers = items.Select((item, index) => new ItemTaskWrapper<TInput>(item, taskSelector, _taskCompletionSources.GetOrAdd(index, new TaskCompletionSource())));
    }
}