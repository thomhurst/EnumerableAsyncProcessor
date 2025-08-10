#if NET6_0_OR_GREATER
using System.Collections.Concurrent;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable.ResultProcessors;

public class ResultAsyncEnumerableParallelProcessor<TInput, TOutput> : IAsyncEnumerableProcessor<TOutput>
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task<TOutput>> _taskSelector;
    private readonly int _maxConcurrency;
    private readonly CancellationTokenSource _cancellationTokenSource;

    internal ResultAsyncEnumerableParallelProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task<TOutput>> taskSelector,
        int maxConcurrency,
        CancellationTokenSource cancellationTokenSource)
    {
        _items = items;
        _taskSelector = taskSelector;
        _maxConcurrency = maxConcurrency;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public async IAsyncEnumerable<TOutput> ExecuteAsync()
    {
        var cancellationToken = _cancellationTokenSource.Token;
        using var semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
        var tasks = new List<Task<TOutput>>();

        try
        {
            await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                
                var capturedItem = item;
                // Use Task.Run to ensure parallelism and prevent blocking
                var task = Task.Run(async () => 
                {
                    try
                    {
                        return await _taskSelector(capturedItem).ConfigureAwait(false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken);
                tasks.Add(task);
                
                // Yield completed results
                while (tasks.Count > 0 && tasks[0].IsCompleted)
                {
                    var completedTask = tasks[0];
                    tasks.RemoveAt(0);
                    yield return await completedTask.ConfigureAwait(false);
                }
            }

            // Yield remaining results
            foreach (var task in tasks)
            {
                yield return await task.ConfigureAwait(false);
            }
        }
        finally
        {
            // Ensure all tasks complete before the using block disposes the semaphore
            // This handles cancellation or exception scenarios
            if (tasks.Count > 0)
            {
                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch
                {
                    // Ignore exceptions here as they've already been handled
                }
            }
        }
    }
}
#endif