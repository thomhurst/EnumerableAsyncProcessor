using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

/// <summary>
/// A specialized parallel processor that starts ALL tasks immediately without any concurrency limits and returns results.
/// WARNING: Use with caution - this can overwhelm system resources with large task counts.
/// Ideal for scenarios where you need maximum parallelism and have sufficient resources.
/// </summary>
public class ResultUnboundedParallelAsyncProcessor<TOutput> : ResultAbstractAsyncProcessor<TOutput>
{
    internal ResultUnboundedParallelAsyncProcessor(int count, Func<Task<TOutput>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
    }

    internal override Task Process()
    {
        // Start ALL tasks immediately without any throttling
        // This provides true unbounded parallelism
        var tasks = TaskWrappers.Select(taskWrapper => 
        {
            var task = taskWrapper.Process(CancellationToken);
            // Fast-path for already completed tasks
            if (task.IsCompleted)
            {
                return task;
            }
            return task;
        });

        return Task.WhenAll(tasks);
    }
}