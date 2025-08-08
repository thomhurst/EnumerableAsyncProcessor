#if NET6_0_OR_GREATER
using System.Threading.Channels;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable.ResultProcessors;

/// <summary>
/// Channel-based processor that returns results with configurable ordering.
/// </summary>
public class ResultAsyncEnumerableChannelBasedProcessor<TInput, TOutput> : IAsyncEnumerableProcessor<TOutput>
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task<TOutput>> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly AsyncEnumerableChannelOptions _options;

    internal ResultAsyncEnumerableChannelBasedProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task<TOutput>> taskSelector,
        CancellationTokenSource cancellationTokenSource,
        AsyncEnumerableChannelOptions options)
    {
        _items = items;
        _taskSelector = taskSelector;
        _cancellationTokenSource = cancellationTokenSource;
        _options = options;
    }

    public async IAsyncEnumerable<TOutput> ExecuteAsync()
    {
        var cancellationToken = _cancellationTokenSource.Token;

        if (_options.PreserveOrder)
        {
            await foreach (var result in ExecuteWithOrderPreservationAsync(cancellationToken))
            {
                yield return result;
            }
        }
        else
        {
            await foreach (var result in ExecuteWithoutOrderPreservationAsync(cancellationToken))
            {
                yield return result;
            }
        }
    }

    private async IAsyncEnumerable<TOutput> ExecuteWithoutOrderPreservationAsync(CancellationToken cancellationToken)
    {
        var inputChannel = CreateInputChannel();
        var outputChannel = Channel.CreateUnbounded<TOutput>();

        // Producer task
        var producerTask = ProduceAsync(inputChannel.Writer, cancellationToken);

        // Consumer tasks
        var consumerTasks = Enumerable.Range(0, _options.MaxConcurrency)
            .Select(_ => ConsumeAsync(inputChannel.Reader, outputChannel.Writer, cancellationToken))
            .ToArray();

        // Complete output when all processing is done
        var completionTask = Task.Run(async () =>
        {
            await producerTask;
            await Task.WhenAll(consumerTasks);
            outputChannel.Writer.Complete();
        }, cancellationToken);

        // Yield results as they complete
        await foreach (var result in outputChannel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return result;
        }

        await completionTask;
    }

    private async IAsyncEnumerable<TOutput> ExecuteWithOrderPreservationAsync(CancellationToken cancellationToken)
    {
        var outputChannel = Channel.CreateUnbounded<TOutput>();
        var semaphore = new SemaphoreSlim(_options.MaxConcurrency, _options.MaxConcurrency);
        var orderingDictionary = new SortedDictionary<int, TaskCompletionSource<TOutput>>();
        var orderingLock = new SemaphoreSlim(1, 1);
        var totalItemsProduced = 0;
        var nextYieldIndex = 0;

        // Start producer
        var orderedInputChannel = CreateOrderedInputChannel();
        var producerTask = ProduceOrderedAsync(orderedInputChannel.Writer, cancellationToken);

        // Start ordered consumer
        var consumerTask = Task.Run(async () =>
        {
            await foreach (var (item, index) in orderedInputChannel.Reader.ReadAllAsync(cancellationToken))
            {
                await semaphore.WaitAsync(cancellationToken);
                Interlocked.Increment(ref totalItemsProduced);
                _ = ProcessOrderedItemAsync(item, index, orderingDictionary, orderingLock, semaphore, cancellationToken);
            }
        }, cancellationToken);

        // Yield results in order
        var yieldingTask = Task.Run(async () =>
        {
            while (!producerTask.IsCompleted || nextYieldIndex < totalItemsProduced)
            {
                await orderingLock.WaitAsync(cancellationToken);
                TaskCompletionSource<TOutput>? tcs = null;
                var found = false;
                
                if (orderingDictionary.TryGetValue(nextYieldIndex, out tcs))
                {
                    orderingDictionary.Remove(nextYieldIndex);
                    nextYieldIndex++;
                    found = true;
                }
                orderingLock.Release();
                
                if (found && tcs != null)
                {
                    await outputChannel.Writer.WriteAsync(await tcs.Task, cancellationToken);
                }
                else
                {
                    await Task.Delay(10, cancellationToken);
                }
            }
        }, cancellationToken);
        
        // Yield from output channel
        await foreach (var result in outputChannel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return result;
        }

        await producerTask;
        await consumerTask;
        await yieldingTask;

        // Write any remaining results to output channel
        foreach (var kvp in orderingDictionary)
        {
            await outputChannel.Writer.WriteAsync(await kvp.Value.Task, cancellationToken);
        }
        
        outputChannel.Writer.Complete();
    }

    private async Task ProcessOrderedItemAsync(
        TInput item,
        int index,
        SortedDictionary<int, TaskCompletionSource<TOutput>> orderingDictionary,
        SemaphoreSlim orderingLock,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _taskSelector(item);

            await orderingLock.WaitAsync(cancellationToken);
            try
            {
                var tcs = new TaskCompletionSource<TOutput>();
                tcs.SetResult(result);
                orderingDictionary[index] = tcs;
            }
            finally
            {
                orderingLock.Release();
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    private Channel<TInput> CreateInputChannel()
    {
        return _options.BufferSize.HasValue
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
    }

    private Channel<(TInput, int)> CreateOrderedInputChannel()
    {
        return _options.BufferSize.HasValue
            ? Channel.CreateBounded<(TInput, int)>(new BoundedChannelOptions(_options.BufferSize.Value)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = true
            })
            : Channel.CreateUnbounded<(TInput, int)>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = true
            });
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
        finally
        {
            writer.Complete();
        }
    }

    private async Task ProduceOrderedAsync(ChannelWriter<(TInput, int)> writer, CancellationToken cancellationToken)
    {
        try
        {
            var index = 0;
            await foreach (var item in _items.WithCancellation(cancellationToken))
            {
                await writer.WriteAsync((item, index++), cancellationToken);
            }
        }
        finally
        {
            writer.Complete();
        }
    }

    private async Task ConsumeAsync(
        ChannelReader<TInput> reader,
        ChannelWriter<TOutput> writer,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_options.IsIOBound)
            {
                await foreach (var item in reader.ReadAllAsync(cancellationToken))
                {
                    var result = await _taskSelector(item);
                    await writer.WriteAsync(result, cancellationToken);
                }
            }
            else
            {
                await Task.Run(async () =>
                {
                    await foreach (var item in reader.ReadAllAsync(cancellationToken))
                    {
                        var result = await _taskSelector(item);
                        await writer.WriteAsync(result, cancellationToken);
                    }
                }, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
    }
}
#endif