#if NET6_0_OR_GREATER
using System.Threading.Channels;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable;

public class AsyncEnumerableParallelProcessor<TInput> : IAsyncEnumerableProcessor
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task> _taskSelector;
    private readonly int _maxConcurrency;
    private readonly CancellationTokenSource _cancellationTokenSource;

    internal AsyncEnumerableParallelProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task> taskSelector,
        int maxConcurrency,
        CancellationTokenSource cancellationTokenSource)
    {
        _items = items;
        _taskSelector = taskSelector;
        _maxConcurrency = maxConcurrency;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public async Task ExecuteAsync()
    {
        var channel = Channel.CreateUnbounded<TInput>();
        var cancellationToken = _cancellationTokenSource.Token;

        // Producer task - enumerate the async enumerable and write to channel
        var producerTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var item in _items.WithCancellation(cancellationToken))
                {
                    await channel.Writer.WriteAsync(item, cancellationToken);
                }
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, cancellationToken);

        // Consumer tasks - process items from the channel in parallel
        var consumerTasks = Enumerable.Range(0, _maxConcurrency)
            .Select(_ => Task.Run(async () =>
            {
                await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    await _taskSelector(item);
                }
            }, cancellationToken))
            .ToArray();

        // Wait for producer and all consumers to complete
        await producerTask;
        await Task.WhenAll(consumerTasks);
    }
}
#endif