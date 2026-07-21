using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;

namespace EnumerableAsyncProcessor;

/// <summary>
/// Processes asynchronous sources with a bounded channel and a fixed set of workers.
/// Source read-ahead and queued results stay proportional to worker count.
/// </summary>
internal static class AsyncEnumerableWorkerPool
{
    internal static async Task ProcessAsync<TInput>(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task> taskSelector,
        int workerCount,
        CancellationToken cancellationToken)
    {
        using var pipelineCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pipelineToken = pipelineCancellation.Token;
        var channel = CreateChannel<TInput>(workerCount);
        var exceptions = new ConcurrentQueue<Exception>();
        var wasCanceled = 0;
        var workers = StartWorkers(channel.Reader, taskSelector, workerCount, exceptions, () => Interlocked.Exchange(ref wasCanceled, 1), pipelineToken);

        try
        {
            try
            {
                await foreach (var item in items.WithCancellation(pipelineToken).ConfigureAwait(false))
                {
                    await channel.Writer.WriteAsync(item, pipelineToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                Interlocked.Exchange(ref wasCanceled, 1);
            }
            catch (Exception exception)
            {
                exceptions.Enqueue(exception);
            }
            finally
            {
                channel.Writer.TryComplete();
            }

            await Task.WhenAll(workers).ConfigureAwait(false);

            ThrowIfFailed(exceptions, wasCanceled, cancellationToken);
        }
        finally
        {
            pipelineCancellation.Cancel();
            channel.Writer.TryComplete();
        }
    }

    internal static async IAsyncEnumerable<TOutput> ProcessResultsAsync<TInput, TOutput>(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task<TOutput>> taskSelector,
        int workerCount,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var pipelineCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pipelineToken = pipelineCancellation.Token;
        var channel = CreateChannel<ResultWorkItem<TInput, TOutput>>(workerCount);
        var workers = StartResultWorkers(channel.Reader, taskSelector, workerCount, pipelineToken);
        var pendingResults = new Queue<Task<TOutput>>(workerCount);

        try
        {
            await foreach (var item in items.WithCancellation(pipelineToken).ConfigureAwait(false))
            {
                var completionSource = new TaskCompletionSource<TOutput>(TaskCreationOptions.RunContinuationsAsynchronously);
                await channel.Writer.WriteAsync(new ResultWorkItem<TInput, TOutput>(item, completionSource), pipelineToken).ConfigureAwait(false);
                pendingResults.Enqueue(completionSource.Task);

                if (pendingResults.Count == workerCount)
                {
                    yield return await pendingResults.Dequeue().ConfigureAwait(false);
                }
            }

            channel.Writer.TryComplete();

            while (pendingResults.TryDequeue(out var resultTask))
            {
                yield return await resultTask.ConfigureAwait(false);
            }

            await Task.WhenAll(workers).ConfigureAwait(false);
        }
        finally
        {
            pipelineCancellation.Cancel();
            channel.Writer.TryComplete();

            try
            {
                await Task.WhenAll(workers).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (pipelineToken.IsCancellationRequested)
            {
                // Expected when enumeration is canceled or the consumer stops early.
            }
        }
    }

    private static Channel<T> CreateChannel<T>(int capacity)
    {
        return Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
        {
            SingleWriter = true,
            SingleReader = false,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        });
    }

    private static Task[] StartWorkers<TInput>(
        ChannelReader<TInput> reader,
        Func<TInput, Task> taskSelector,
        int workerCount,
        ConcurrentQueue<Exception> exceptions,
        Action recordCancellation,
        CancellationToken cancellationToken)
    {
        var workers = new Task[workerCount];

        for (var i = 0; i < workerCount; i++)
        {
            workers[i] = Task.Run(async () =>
            {
                await foreach (var item in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    Task? task = null;

                    try
                    {
                        task = taskSelector(item);
                        await task.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        recordCancellation();
                    }
                    catch (Exception exception)
                    {
                        EnqueueExceptions(exceptions, task, exception);
                    }
                }
            }, cancellationToken);
        }

        return workers;
    }

    private static Task[] StartResultWorkers<TInput, TOutput>(
        ChannelReader<ResultWorkItem<TInput, TOutput>> reader,
        Func<TInput, Task<TOutput>> taskSelector,
        int workerCount,
        CancellationToken cancellationToken)
    {
        var workers = new Task[workerCount];

        for (var i = 0; i < workerCount; i++)
        {
            workers[i] = Task.Run(async () =>
            {
                await foreach (var workItem in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    Task<TOutput>? task = null;

                    try
                    {
                        task = taskSelector(workItem.Input);
                        workItem.CompletionSource.TrySetResult(await task.ConfigureAwait(false));
                    }
                    catch (Exception exception)
                    {
                        workItem.CompletionSource.TrySetFromFault(task, exception, cancellationToken);
                    }
                }
            }, cancellationToken);
        }

        return workers;
    }

    private static void EnqueueExceptions(
        ConcurrentQueue<Exception> exceptions,
        Task? task,
        Exception exception)
    {
        if (task is { IsFaulted: true })
        {
            foreach (var innerException in task.Exception!.InnerExceptions)
            {
                exceptions.Enqueue(innerException);
            }

            return;
        }

        exceptions.Enqueue(exception);
    }

    private static void ThrowIfFailed(
        ConcurrentQueue<Exception> exceptions,
        int wasCanceled,
        CancellationToken cancellationToken)
    {
        if (exceptions.TryDequeue(out var firstException))
        {
            ExceptionDispatchInfo.Capture(firstException).Throw();
        }

        if (wasCanceled != 0)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private readonly record struct ResultWorkItem<TInput, TOutput>(
        TInput Input,
        TaskCompletionSource<TOutput> CompletionSource);
}
