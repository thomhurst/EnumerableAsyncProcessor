using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable;

public class AsyncEnumerableParallelProcessor<TInput> : IAsyncEnumerableProcessor
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task> _taskSelector;
    private readonly int? _maxConcurrency;
    private readonly bool _scheduleOnThreadPool;
    private readonly CancellationTokenSource _cancellationTokenSource;

    internal AsyncEnumerableParallelProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task> taskSelector,
        int? maxConcurrency,
        bool scheduleOnThreadPool,
        CancellationTokenSource cancellationTokenSource)
    {
        _items = items;
        _taskSelector = taskSelector;
        _maxConcurrency = maxConcurrency;
        _scheduleOnThreadPool = scheduleOnThreadPool;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public async Task ExecuteAsync()
    {
        var cancellationToken = _cancellationTokenSource.Token;
        
        if (_maxConcurrency.HasValue)
        {
            await AsyncEnumerableWorkerPool.ProcessAsync(
                _items,
                _taskSelector,
                _maxConcurrency.Value,
                cancellationToken).ConfigureAwait(false);

            return;
        }
        else
        {
            // Unbounded parallel processing
            var tasks = new List<Task>();

            try
            {
                await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    var capturedItem = item;

                    Task task;
                    if (_scheduleOnThreadPool)
                    {
                        task = Task.Run(() => _taskSelector(capturedItem), cancellationToken);
                    }
                    else
                    {
                        task = _taskSelector(capturedItem);
                    }

                    tasks.Add(task);
                }
            }
            finally
            {
                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
            }
        }
    }
}
