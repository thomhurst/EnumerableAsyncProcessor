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
            await Task.WhenAll(TaskWrappers.Select(taskWrapper => taskWrapper.Process(CancellationToken))).ConfigureAwait(false);
            return;
        }

        // Use semaphore for concurrency throttling
        using var semaphore = new SemaphoreSlim(_maxConcurrency.Value, _maxConcurrency.Value);
        
        var tasks = TaskWrappers.Select(async taskWrapper =>
        {
            await semaphore.WaitAsync(CancellationToken).ConfigureAwait(false);
            try
            {
                var task = taskWrapper.Process(CancellationToken);
                
                // Fast-path for already completed tasks
                if (task.IsCompleted)
                {
                    return;
                }
                
                await task.ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}