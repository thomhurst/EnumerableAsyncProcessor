using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

/// <summary>
/// A specialized parallel processor that starts ALL tasks immediately without any concurrency limits and returns results.
/// WARNING: Use with extreme caution - this WILL overwhelm system resources with large task counts.
/// 
/// RISKS:
/// - No concurrency throttling - all tasks start immediately
/// - Can cause thread pool starvation with thousands of tasks
/// - May exhaust system memory with very large collections
/// - Can lead to degraded system performance
/// 
/// RECOMMENDATIONS:
/// - For collections > 1000 items, use ResultParallelAsyncProcessor with appropriate concurrency limits
/// - Monitor system resources when using this processor
/// - Consider the bounded alternatives for production use
/// 
/// Ideal only for scenarios with:
/// - Small task counts (< 100)
/// - Very lightweight operations
/// - Sufficient system resources
/// - Controlled environments where task count is known
/// </summary>
public class ResultUnboundedParallelAsyncProcessor<TOutput> : ResultAbstractAsyncProcessor<TOutput>
{
    internal ResultUnboundedParallelAsyncProcessor(int count, Func<Task<TOutput>> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
    }

    internal override async Task Process()
    {
        // Start ALL tasks immediately without any throttling
        // This provides true unbounded parallelism
        var tasks = TaskWrappers.Select(taskWrapper => taskWrapper.Process(CancellationToken));

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}