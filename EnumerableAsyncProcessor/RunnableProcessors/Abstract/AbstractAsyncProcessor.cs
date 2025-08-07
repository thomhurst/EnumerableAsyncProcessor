using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors.Abstract;

public abstract class AbstractAsyncProcessor : AbstractAsyncProcessorBase
{
    private readonly ConcurrentDictionary<int, TaskCompletionSource> _taskCompletionSources = [];

    protected readonly IEnumerable<ActionTaskWrapper> TaskWrappers;

    [field: AllowNull, MaybeNull]
    protected override IEnumerable<TaskCompletionSource> EnumerableTaskCompletionSources
        => field ??= TaskWrappers.Select(x => x.TaskCompletionSource);

    
    protected AbstractAsyncProcessor(int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(cancellationTokenSource)
    {
        ValidationHelper.ValidateCount(count);
        ValidationHelper.ThrowIfNull(taskSelector);

        // Provide optimization for empty collections
        if (count == 0)
        {
            TaskWrappers = [];
            return;
        }

        // Provide performance warning for very large collections
        var warning = ValidationHelper.GetPerformanceWarning(count);
        if (warning != null)
        {
            // In a real application, you might want to log this warning
            // For now, we'll just store it as a comment that could be used by logging
            _ = warning;
        }

        TaskWrappers = Enumerable.Range(0, count).Select(index => new ActionTaskWrapper(taskSelector, _taskCompletionSources.GetOrAdd(index, new TaskCompletionSource())));
    }
}