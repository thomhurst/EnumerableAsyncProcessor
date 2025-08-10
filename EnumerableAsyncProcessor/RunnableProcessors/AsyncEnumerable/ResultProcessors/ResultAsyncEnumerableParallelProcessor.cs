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
        var semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
        var tasks = new List<Task<TOutput>>();

        try
        {
            await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                
                var capturedItem = item;
                var task = ProcessItemAsync(capturedItem, semaphore, cancellationToken);
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
            semaphore.Dispose();
        }
    }

    private async Task<TOutput> ProcessItemAsync(
        TInput item, 
        SemaphoreSlim semaphore, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Yield to ensure we don't block the thread if _taskSelector is synchronous
            await Task.Yield();
            return await _taskSelector(item).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
#endif