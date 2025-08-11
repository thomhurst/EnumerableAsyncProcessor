using EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class ParallelAsyncProcessor : AbstractAsyncProcessor
{
    private readonly int? _maxConcurrency;
    private readonly bool _scheduleOnThreadPool;
    
    internal ParallelAsyncProcessor(int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource, int? maxConcurrency = null, bool scheduleOnThreadPool = false) : base(count, taskSelector, cancellationTokenSource)
    {
        _maxConcurrency = maxConcurrency;
        _scheduleOnThreadPool = scheduleOnThreadPool;
    }

    internal override async Task Process()
    {
        // If no concurrency limit, process all tasks in parallel
        if (_maxConcurrency == null)
        {
            if (_scheduleOnThreadPool)
            {
                // Use Task.Run to ensure all tasks start immediately on thread pool threads
                // This prevents synchronous code in user delegates from blocking other tasks
                // Small overhead (~1-2μs per task) but necessary for safety with unknown delegates
                await Task.WhenAll(TaskWrappers.Select(taskWrapper => 
                    Task.Run(() => taskWrapper.Process(CancellationToken), CancellationToken)
                )).ConfigureAwait(false);
            }
            else
            {
                // Direct execution for maximum performance when delegates are known to be async
                // WARNING: May cause thread pool starvation if delegates contain blocking code
                await Task.WhenAll(TaskWrappers.Select(taskWrapper => 
                    taskWrapper.Process(CancellationToken)
                )).ConfigureAwait(false);
            }
            return;
        }

        // Use semaphore for concurrency throttling
        using var semaphore = new SemaphoreSlim(_maxConcurrency.Value, _maxConcurrency.Value);
        
        // Materialize tasks immediately to ensure they all start in parallel (up to concurrency limit)
        var tasks = _scheduleOnThreadPool
            ? // Use Task.Run to prevent synchronous code from blocking thread pool threads
              TaskWrappers.Select(taskWrapper => Task.Run(async () =>
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
              }, CancellationToken)).ToList()
            : // Direct execution for maximum performance
              TaskWrappers.Select(async taskWrapper =>
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
              }).ToList(); // Force immediate task creation

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}