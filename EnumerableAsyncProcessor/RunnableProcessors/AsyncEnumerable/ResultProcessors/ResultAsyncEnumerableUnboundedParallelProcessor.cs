#if NET6_0_OR_GREATER
using System.Threading.Channels;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable.ResultProcessors;

/// <summary>
/// A specialized parallel processor that starts ALL tasks immediately without any concurrency limits
/// and returns results as they complete.
/// WARNING: Use with caution - this can overwhelm system resources with large async enumerables.
/// </summary>
public class ResultAsyncEnumerableUnboundedParallelProcessor<TInput, TOutput> : IAsyncEnumerableProcessor<TOutput>
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task<TOutput>> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    internal ResultAsyncEnumerableUnboundedParallelProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task<TOutput>> taskSelector,
        CancellationTokenSource cancellationTokenSource)
    {
        _items = items;
        _taskSelector = taskSelector;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public async IAsyncEnumerable<TOutput> ExecuteAsync()
    {
        var cancellationToken = _cancellationTokenSource.Token;
        var outputChannel = Channel.CreateUnbounded<TOutput>();
        
        // Start processing task
        var processingTask = ProcessAsync(outputChannel.Writer, cancellationToken);

        // Yield results as they become available
        await foreach (var result in outputChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return result;
        }

        // Ensure processing completes
        await processingTask.ConfigureAwait(false);
    }

    private async Task ProcessAsync(ChannelWriter<TOutput> writer, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();

        try
        {
            // Start a task for each item immediately as it arrives
            await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                // Capture the item in a local variable for the closure
                var capturedItem = item;
                
                // Start task immediately and write result when complete
                var task = Task.Run(async () =>
                {
                    try
                    {
                        var result = await _taskSelector(capturedItem).ConfigureAwait(false);
                        await writer.WriteAsync(result, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected on cancellation
                    }
                }, cancellationToken);
                
                tasks.Add(task);
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        finally
        {
            writer.Complete();
        }
    }
}
#endif