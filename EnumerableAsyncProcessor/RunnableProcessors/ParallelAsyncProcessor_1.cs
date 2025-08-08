using EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class ParallelAsyncProcessor<TInput> : AbstractAsyncProcessor<TInput>
{
    private readonly bool _isIOBound;
    
    internal ParallelAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task> taskSelector, CancellationTokenSource cancellationTokenSource, bool isIOBound = true) : base(items, taskSelector, cancellationTokenSource)
    {
        _isIOBound = isIOBound;
    }

    internal override async Task Process()
    {
        // For I/O-bound tasks, don't use Task.Run wrapper as it adds unnecessary overhead
        // The tasks are already async and won't block threads
        if (_isIOBound)
        {
            await Task.WhenAll(TaskWrappers.Select(taskWrapper => 
            {
                var task = taskWrapper.Process(CancellationToken);
                // Fast-path for already completed tasks
                if (task.IsCompleted)
                {
                    return task;
                }
                return task;
            })).ConfigureAwait(false);
            return;
        }

        // For CPU-bound tasks, use Task.Run to offload to ThreadPool
        await Task.WhenAll(TaskWrappers.Select(taskWrapper => Task.Run(() => taskWrapper.Process(CancellationToken)))).ConfigureAwait(false);
    }
}