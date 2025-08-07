using EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class ParallelAsyncProcessor : AbstractAsyncProcessor
{
    private readonly bool _isIOBound;
    
    internal ParallelAsyncProcessor(int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource, bool isIOBound = true) : base(count, taskSelector, cancellationTokenSource)
    {
        _isIOBound = isIOBound;
    }

    internal override Task Process()
    {
        // For I/O-bound tasks, don't use Task.Run wrapper as it adds unnecessary overhead
        // The tasks are already async and won't block threads
        if (_isIOBound)
        {
            return Task.WhenAll(TaskWrappers.Select(taskWrapper => 
            {
                var task = taskWrapper.Process(CancellationToken);
                // Fast-path for already completed tasks
                if (task.IsCompleted)
                {
                    return task;
                }
                return task;
            }));
        }

        // For CPU-bound tasks, use Task.Run to offload to ThreadPool
        return Task.WhenAll(TaskWrappers.Select(taskWrapper => Task.Run(() => taskWrapper.Process(CancellationToken))));
    }
}