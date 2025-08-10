using EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors;

/// <summary>
/// A specialized parallel processor that starts ALL tasks immediately without any concurrency limits.
/// WARNING: Use with caution - this can overwhelm system resources with large task counts.
/// Ideal for scenarios where you need maximum parallelism and have sufficient resources.
/// </summary>
public class UnboundedParallelAsyncProcessor<TInput> : AbstractAsyncProcessor<TInput>
{
    internal UnboundedParallelAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
    }

    internal override async Task Process()
    {
        // Start ALL tasks immediately without any throttling
        // This provides true unbounded parallelism
        var tasks = TaskWrappers.Select(taskWrapper => 
        {
            var task = taskWrapper.Process(CancellationToken);
            // Fast-path for already completed tasks
            if (task.IsCompleted)
            {
            }
            return task;
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}