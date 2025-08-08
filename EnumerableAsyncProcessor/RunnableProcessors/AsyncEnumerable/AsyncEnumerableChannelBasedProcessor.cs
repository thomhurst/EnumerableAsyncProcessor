#if NET6_0_OR_GREATER
using System.Threading.Channels;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable;

/// <summary>
/// Channel-based processor with configurable buffer size and concurrency for IAsyncEnumerable.
/// Provides excellent backpressure management.
/// </summary>
public class AsyncEnumerableChannelBasedProcessor<TInput> : IAsyncEnumerableProcessor
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly AsyncEnumerableChannelOptions _options;

    internal AsyncEnumerableChannelBasedProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task> taskSelector,
        CancellationTokenSource cancellationTokenSource,
        AsyncEnumerableChannelOptions options)
    {
        _items = items;
        _taskSelector = taskSelector;
        _cancellationTokenSource = cancellationTokenSource;
        _options = options;
    }

    public async Task ExecuteAsync()
    {
        var cancellationToken = _cancellationTokenSource.Token;
        
        // Create channel with specified buffer size
        var channel = _options.BufferSize.HasValue
            ? Channel.CreateBounded<TInput>(new BoundedChannelOptions(_options.BufferSize.Value)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = true
            })
            : Channel.CreateUnbounded<TInput>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = true
            });

        // Producer task
        var producerTask = ProduceAsync(channel.Writer, cancellationToken);

        // Consumer tasks
        var consumerTasks = Enumerable.Range(0, _options.MaxConcurrency)
            .Select(_ => ConsumeAsync(channel.Reader, cancellationToken))
            .ToArray();

        // Wait for all tasks
        await producerTask;
        await Task.WhenAll(consumerTasks);
    }

    private async Task ProduceAsync(ChannelWriter<TInput> writer, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var item in _items.WithCancellation(cancellationToken))
            {
                await writer.WriteAsync(item, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        finally
        {
            writer.Complete();
        }
    }

    private async Task ConsumeAsync(ChannelReader<TInput> reader, CancellationToken cancellationToken)
    {
        if (_options.IsIOBound)
        {
            // For I/O-bound tasks, process directly without Task.Run
            await foreach (var item in reader.ReadAllAsync(cancellationToken))
            {
                await _taskSelector(item);
            }
        }
        else
        {
            // For CPU-bound tasks, use Task.Run to avoid blocking
            await Task.Run(async () =>
            {
                await foreach (var item in reader.ReadAllAsync(cancellationToken))
                {
                    await _taskSelector(item);
                }
            }, cancellationToken);
        }
    }
}
#endif