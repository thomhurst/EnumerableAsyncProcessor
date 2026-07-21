using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace EnumerableAsyncProcessor;

/// <summary>
/// Processes asynchronous sources with a bounded channel and a fixed set of workers.
/// Source read-ahead and queued results stay proportional to worker count.
/// </summary>
internal static class AsyncEnumerableWorkerPool
{
    // Returned via a TaskCompletionSource rather than as the async method's own task so the
    // result carries Task.WhenAll fidelity: awaiting it throws the first failure while
    // Task.Exception.InnerExceptions preserves every queued failure.
    internal static Task ProcessAsync<TInput>(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task> taskSelector,
        int workerCount,
        CancellationToken cancellationToken)
    {
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = RunAsync();
        return completionSource.Task;

        async Task RunAsync()
        {
            try
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

                    try
                    {
                        await Task.WhenAll(workers).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        Interlocked.Exchange(ref wasCanceled, 1);
                    }

                    if (!exceptions.IsEmpty)
                    {
                        completionSource.TrySetException(exceptions);
                    }
                    else if (wasCanceled != 0)
                    {
                        completionSource.TrySetCanceled(
                            cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(canceled: true));
                    }
                    else
                    {
                        completionSource.TrySetResult();
                    }
                }
                finally
                {
                    pipelineCancellation.Cancel();
                    channel.Writer.TryComplete();
                }
            }
            catch (Exception exception)
            {
                // Defensive: nothing above should throw, but the caller's task must always complete.
                completionSource.TrySetException(exception);
            }
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
                    // WaitAsync guards against a worker observing cancellation and exiting
                    // between this item being written and it being claimed - the item's
                    // completion source would otherwise never complete and this await
                    // would hang forever.
                    var value = await pendingResults.Peek().WaitAsync(pipelineToken).ConfigureAwait(false);
                    pendingResults.Dequeue();
                    yield return value;
                }
            }

            channel.Writer.TryComplete();

            while (pendingResults.Count > 0)
            {
                var value = await pendingResults.Peek().WaitAsync(pipelineToken).ConfigureAwait(false);
                pendingResults.Dequeue();
                yield return value;
            }

            await Task.WhenAll(workers).ConfigureAwait(false);
        }
        finally
        {
            pipelineCancellation.Cancel();
            channel.Writer.TryComplete();

            try
            {
                // Bounded: a non-cancellation-aware in-flight item must not block iterator
                // disposal indefinitely when the consumer abandons the stream early.
                await Task.WhenAll(workers).WaitAsync(ProcessorLifecycle.DisposalTimeout).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (pipelineToken.IsCancellationRequested)
            {
                // Expected when enumeration is canceled or the consumer stops early.
            }
            catch (TimeoutException)
            {
                // Work still running after the disposal window; abandoned results below stay observed.
            }

            // Results abandoned by cancellation or an earlier failure may still fault;
            // observe them so they cannot surface as UnobservedTaskException.
            while (pendingResults.TryDequeue(out var abandonedResult))
            {
                _ = abandonedResult.ContinueWith(
                    static t => _ = t.Exception,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
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
                try
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
                }
                catch (OperationCanceledException)
                {
                    // Cancellation can strand items that were written but never claimed;
                    // complete them so nothing awaiting their results hangs.
                    while (reader.TryRead(out var abandonedItem))
                    {
                        abandonedItem.CompletionSource.TrySetCanceled(cancellationToken);
                    }

                    throw;
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

    private readonly record struct ResultWorkItem<TInput, TOutput>(
        TInput Input,
        TaskCompletionSource<TOutput> CompletionSource);
}
