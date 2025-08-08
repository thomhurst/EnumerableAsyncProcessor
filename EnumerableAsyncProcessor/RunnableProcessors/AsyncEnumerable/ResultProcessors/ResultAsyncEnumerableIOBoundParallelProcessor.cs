#if NET6_0_OR_GREATER
using System.Threading.Channels;
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable.ResultProcessors;

/// <summary>
/// Optimized parallel processor for I/O-bound operations that returns results.
/// </summary>
public class ResultAsyncEnumerableIOBoundParallelProcessor<TInput, TOutput> : IAsyncEnumerableProcessor<TOutput>
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task<TOutput>> _taskSelector;
    private readonly int _maxConcurrency;
    private readonly CancellationTokenSource _cancellationTokenSource;

    internal ResultAsyncEnumerableIOBoundParallelProcessor(
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
        var outputChannel = Channel.CreateUnbounded<TOutput>();

        // Start processing in background
        var processingTask = ProcessAsync(outputChannel.Writer, cancellationToken);

        // Yield results as they complete
        await foreach (var result in outputChannel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return result;
        }

        await processingTask;
    }

    private async Task ProcessAsync(ChannelWriter<TOutput> writer, CancellationToken cancellationToken)
    {
        var semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
        var tasks = new List<Task>();

        try
        {
            await foreach (var item in _items.WithCancellation(cancellationToken))
            {
                await semaphore.WaitAsync(cancellationToken);

                // For I/O-bound, don't use Task.Run
                var task = ProcessItemAsync(item, writer, semaphore, cancellationToken);
                tasks.Add(task);

                // Periodic cleanup of completed tasks
                if (tasks.Count > _maxConcurrency * 2)
                {
                    tasks.RemoveAll(t => t.IsCompleted);
                }
            }

            await Task.WhenAll(tasks);
        }
        finally
        {
            writer.Complete();
            semaphore.Dispose();
        }
    }

    private async Task ProcessItemAsync(
        TInput item,
        ChannelWriter<TOutput> writer,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _taskSelector(item);
            await writer.WriteAsync(result, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
        finally
        {
            semaphore.Release();
        }
    }
}
#endif