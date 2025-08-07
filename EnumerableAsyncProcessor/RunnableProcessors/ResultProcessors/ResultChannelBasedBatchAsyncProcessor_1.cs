#if NET6_0_OR_GREATER
using System.Threading.Channels;
using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors;

/// <summary>
/// Channel-based batch processor for result tasks, providing producer-consumer pattern with backpressure support.
/// </summary>
public class ResultChannelBasedBatchAsyncProcessor<TOutput> : ResultAbstractAsyncProcessor<TOutput>
{
    private readonly ChannelProcessorOptions _options;

    internal ResultChannelBasedBatchAsyncProcessor(
        int count, 
        Func<Task<TOutput>> taskSelector, 
        CancellationTokenSource cancellationTokenSource,
        ChannelProcessorOptions? options = null) 
        : base(count, taskSelector, cancellationTokenSource)
    {
        _options = options ?? ChannelProcessorOptions.CreateUnbounded();
    }

    internal override async Task Process()
    {
        // Create the appropriate channel type
        Channel<ActionTaskWrapper<TOutput>> channel;
        if (_options.Capacity.HasValue)
        {
            channel = Channel.CreateBounded<ActionTaskWrapper<TOutput>>(_options.CreateBoundedChannelOptions());
        }
        else
        {
            channel = Channel.CreateUnbounded<ActionTaskWrapper<TOutput>>(_options.CreateUnboundedChannelOptions());
        }

        var writer = channel.Writer;
        var reader = channel.Reader;

        // Start producer task
        var producerTask = Task.Run(async () =>
        {
            try
            {
                foreach (var taskWrapper in TaskWrappers)
                {
                    if (CancellationToken.IsCancellationRequested)
                        break;

                    await writer.WriteAsync(taskWrapper, CancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
            {
                // Expected when cancelled
            }
            finally
            {
                // Do not complete here - let the main process handle completion
            }
        }, CancellationToken);

        // Start consumer tasks
        var consumerTasks = new Task[_options.ConsumerCount];
        for (int i = 0; i < _options.ConsumerCount; i++)
        {
            consumerTasks[i] = Task.Run(async () =>
            {
                try
                {
                    await foreach (var taskWrapper in reader.ReadAllAsync(CancellationToken).ConfigureAwait(false))
                    {
                        if (CancellationToken.IsCancellationRequested)
                            break;

                        await taskWrapper.Process(CancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
                {
                    // Expected when cancelled
                }
            }, CancellationToken);
        }

        try
        {
            // Wait for producer to finish
            await producerTask.ConfigureAwait(false);
            
            // Signal completion to consumers
            writer.Complete();

            // Wait for all consumers to finish
            await Task.WhenAll(consumerTasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
        {
            // Expected when cancelled - ensure writer is completed
            writer.TryComplete();
            throw;
        }
        catch (Exception ex)
        {
            // Complete the channel with exception to signal consumers
            writer.TryComplete(ex);
            throw;
        }
    }
}
#endif