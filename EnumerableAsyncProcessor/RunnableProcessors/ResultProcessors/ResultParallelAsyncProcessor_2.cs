using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

public class ResultParallelAsyncProcessor<TInput, TOutput> : ResultAbstractAsyncProcessor<TInput, TOutput>
{
    private readonly bool _isIOBound;
    
    internal ResultParallelAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task<TOutput>> taskSelector, CancellationTokenSource cancellationTokenSource, bool isIOBound = true) : base(items, taskSelector, cancellationTokenSource)
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