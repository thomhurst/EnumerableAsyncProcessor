using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultParallelAsyncProcessor<TInput, TOutput> : ResultAbstractAsyncProcessor<TInput, TOutput>
{
    private readonly int? _maxConcurrency;
    
    internal ResultParallelAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task<TOutput>> taskSelector, CancellationTokenSource cancellationTokenSource, int? maxConcurrency = null) : base(items, taskSelector, cancellationTokenSource)
    {
        _maxConcurrency = maxConcurrency;
    }

    internal override async Task Process()
    {
        // If no concurrency limit, process all tasks in parallel
        if (_maxConcurrency == null)
        {
            await Task.WhenAll(TaskWrappers.Select(taskWrapper => 
            {
                var task = taskWrapper.Process(CancellationToken);
                // Fast-path for already completed tasks
                if (task.IsCompleted)
                {
                }
                return task;
            })).ConfigureAwait(false);
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