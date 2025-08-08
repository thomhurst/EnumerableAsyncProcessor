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
            await foreach (var result in ExecuteWithOrderPreservationAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return result;
            }
        }
        else
        {
            await foreach (var result in ExecuteWithoutOrderPreservationAsync(cancellationToken).ConfigureAwait(false))
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
            await producerTask.ConfigureAwait(false);
            await Task.WhenAll(consumerTasks).ConfigureAwait(false);
            outputChannel.Writer.Complete();
        }, cancellationToken);

        // Yield results as they complete
        await foreach (var result in outputChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return result;
        }

        await completionTask.ConfigureAwait(false);
    }

    private async IAsyncEnumerable<TOutput> ExecuteWithOrderPreservationAsync(CancellationToken cancellationToken)
    {
        var outputChannel = Channel.CreateUnbounded<TOutput>();
        var semaphore = new SemaphoreSlim(_options.MaxConcurrency, _options.MaxConcurrency);
        var orderingDictionary = new SortedDictionary<int, TaskCompletionSource<TOutput>>();
        var orderingLock = new SemaphoreSlim(1, 1);
        var nextYieldIndex = 0;
        var totalProduced = 0;
        var producerCompleted = false;

        // Start producer
        var orderedInputChannel = CreateOrderedInputChannel();
        var producerTask = Task.Run(async () =>
        {
            await ProduceOrderedAsync(orderedInputChannel.Writer, cancellationToken).ConfigureAwait(false);
            producerCompleted = true;
        }, cancellationToken);

        // Start ordered consumer
        var consumerTasks = new List<Task>();
        var consumerTask = Task.Run(async () =>
        {
            await foreach (var (item, index) in orderedInputChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                totalProduced = Math.Max(totalProduced, index + 1);
                var task = ProcessOrderedItemAsync(item, index, orderingDictionary, orderingLock, semaphore, cancellationToken);
                consumerTasks.Add(task);
            }
            await Task.WhenAll(consumerTasks).ConfigureAwait(false);
        }, cancellationToken);

        // Yield results in order
        var yieldingTask = Task.Run(async () =>
        {
            while (!producerCompleted || nextYieldIndex < totalProduced || orderingDictionary.Count > 0)
            {
                await orderingLock.WaitAsync(cancellationToken).ConfigureAwait(false);
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
                    await outputChannel.Writer.WriteAsync(await tcs.Task.ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                }
                else if (producerCompleted && consumerTasks.All(t => t.IsCompleted) && orderingDictionary.Count == 0)
                {
                    break;
                }
                else
                {
                    await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                }
            }
            outputChannel.Writer.Complete();
        }, cancellationToken);
        
        // Yield from output channel
        await foreach (var result in outputChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return result;
        }

        await producerTask.ConfigureAwait(false);
        await consumerTask.ConfigureAwait(false);
        await yieldingTask.ConfigureAwait(false);
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
            var result = await _taskSelector(item).ConfigureAwait(false);

            await orderingLock.WaitAsync(cancellationToken).ConfigureAwait(false);
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
            await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
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
            await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await writer.WriteAsync((item, index++), cancellationToken).ConfigureAwait(false);
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
                await foreach (var item in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    var result = await _taskSelector(item).ConfigureAwait(false);
                    await writer.WriteAsync(result, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                await Task.Run(async () =>
                {
                    await foreach (var item in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var result = await _taskSelector(item).ConfigureAwait(false);
                        await writer.WriteAsync(result, cancellationToken).ConfigureAwait(false);
                    }
                }, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
    }
}
#endif