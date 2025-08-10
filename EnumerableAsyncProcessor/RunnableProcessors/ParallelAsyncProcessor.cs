using EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class ParallelAsyncProcessor : AbstractAsyncProcessor
{
    private readonly int? _maxConcurrency;
    
    internal ParallelAsyncProcessor(int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource, int? maxConcurrency = null) : base(count, taskSelector, cancellationTokenSource)
    {
        _maxConcurrency = maxConcurrency;
    }

    internal override async Task Process()
    {
        // If no concurrency limit, process all tasks in parallel
        if (_maxConcurrency == null)
        {
            // Use Task.Run to ensure all tasks start immediately on thread pool threads
            // This prevents synchronous code in user delegates from blocking other tasks
            await Task.WhenAll(TaskWrappers.Select(taskWrapper => 
                Task.Run(() => taskWrapper.Process(CancellationToken), CancellationToken)
            )).ConfigureAwait(false);
            return;
        }

        // Use semaphore for concurrency throttling
        using var semaphore = new SemaphoreSlim(_maxConcurrency.Value, _maxConcurrency.Value);
        
        // Materialize tasks immediately to ensure they all start in parallel (up to concurrency limit)
        // Use Task.Run to prevent synchronous code from blocking thread pool threads
        var tasks = TaskWrappers.Select(taskWrapper => Task.Run(async () =>
        {
            await semaphore.WaitAsync(CancellationToken).ConfigureAwait(false);
            try
            {
                await taskWrapper.Process(CancellationToken).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }, CancellationToken)).ToList(); // Force immediate task creation

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}