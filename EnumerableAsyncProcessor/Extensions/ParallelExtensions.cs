using System.Collections.Concurrent;

namespace EnumerableAsyncProcessor.Extensions;

/// <summary>
/// Fire-and-await parallel processing over an <see cref="IEnumerable{T}"/> without building a processor object.
/// To collect results, use <c>SelectAsync(...).ProcessInParallel(...)</c> instead.
/// </summary>
public static class ParallelExtensions
{
    /// <summary>
    /// Runs <paramref name="taskSelector"/> for every item on a fixed pool of workers.
    /// </summary>
    /// <typeparam name="TSource">The input item type.</typeparam>
    /// <param name="source">The items to process. The sequence is materialized once before workers start.</param>
    /// <param name="levelOfParallelism">Maximum concurrent operations. Zero or negative uses <see cref="Environment.ProcessorCount"/>.</param>
    /// <param name="taskSelector">The async operation to perform on each item. Any result carried by a returned <c>Task&lt;T&gt;</c> is not collected.</param>
    /// <returns>
    /// A task that completes when every item has been processed. When multiple items fail, awaiting it
    /// throws the first failure while <c>Task.Exception.InnerExceptions</c> carries every failure.
    /// </returns>
    public static Task InParallelAsync<TSource>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task> taskSelector)
    {
        return InParallelAsync(source, levelOfParallelism, taskSelector, CancellationToken.None);
    }

    /// <summary>
    /// Runs <paramref name="taskSelector"/> for every item on a fixed pool of workers.
    /// </summary>
    /// <typeparam name="TSource">The input item type.</typeparam>
    /// <param name="source">The items to process. The sequence is materialized once before workers start.</param>
    /// <param name="levelOfParallelism">Maximum concurrent operations. Zero or negative uses <see cref="Environment.ProcessorCount"/>.</param>
    /// <param name="taskSelector">The async operation to perform on each item. Any result carried by a returned <c>Task&lt;T&gt;</c> is not collected.</param>
    /// <param name="cancellationToken">Stops claiming new items when cancelled. The returned task then completes as canceled, unless items had already failed - their exceptions take precedence.</param>
    /// <returns>
    /// A task that completes when every item has been processed. When multiple items fail, awaiting it
    /// throws the first failure while <c>Task.Exception.InnerExceptions</c> carries every failure.
    /// </returns>
    public static Task InParallelAsync<TSource>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task> taskSelector,
        CancellationToken cancellationToken)
    {
        return StartWorkers(source, levelOfParallelism, taskSelector, cancellationToken);
    }

    /// <summary>
    /// Runs a synchronous, CPU-bound <paramref name="taskSelector"/> for every item on a fixed pool of thread-pool workers.
    /// </summary>
    /// <typeparam name="TSource">The input item type.</typeparam>
    /// <param name="source">The items to process. The sequence is materialized once before workers start.</param>
    /// <param name="levelOfParallelism">Maximum concurrent operations. Zero or negative uses <see cref="Environment.ProcessorCount"/>.</param>
    /// <param name="taskSelector">The synchronous operation to perform on each item.</param>
    /// <param name="cancellationToken">Stops claiming new items when cancelled. The returned task then completes as canceled, unless items had already failed - their exceptions take precedence.</param>
    /// <returns>
    /// A task that completes when every item has been processed. When multiple items fail, awaiting it
    /// throws the first failure while <c>Task.Exception.InnerExceptions</c> carries every failure.
    /// </returns>
    public static Task InParallelAsync<TSource>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Action<TSource> taskSelector,
        CancellationToken cancellationToken = default)
    {
        return StartWorkers(
            source,
            levelOfParallelism,
            item =>
            {
                taskSelector(item);
                return Task.CompletedTask;
            },
            cancellationToken);
    }

    private static Task StartWorkers<TSource>(
        IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task> taskSelector,
        CancellationToken cancellationToken)
    {
        TSource[] items;

        try
        {
            items = source.ToArray();
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }

        if (items.Length == 0)
        {
            return Task.CompletedTask;
        }

        if (levelOfParallelism <= 0)
        {
            levelOfParallelism = Environment.ProcessorCount;
        }

        var workerCount = Math.Min(levelOfParallelism, items.Length);
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var exceptions = new ConcurrentQueue<Exception>();
        var nextIndex = -1;
        var remainingWorkers = workerCount;
        var wasCanceled = 0;

        for (var i = 0; i < workerCount; i++)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            Interlocked.Exchange(ref wasCanceled, 1);
                            return;
                        }

                        var index = Interlocked.Increment(ref nextIndex);

                        if (index >= items.Length)
                        {
                            return;
                        }

                        Task? task = null;

                        try
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            task = taskSelector(items[index]);
                            await task.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            Interlocked.Exchange(ref wasCanceled, 1);
                        }
                        catch (Exception exception)
                        {
                            EnqueueExceptions(exceptions, task, exception);
                        }
                    }
                }
                catch (Exception exception)
                {
                    exceptions.Enqueue(exception);
                }
                finally
                {
                    if (Interlocked.Decrement(ref remainingWorkers) == 0)
                    {
                        Complete(completionSource, exceptions, wasCanceled, cancellationToken);
                    }
                }
            });
        }

        return completionSource.Task;
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

    private static void Complete(
        TaskCompletionSource completionSource,
        ConcurrentQueue<Exception> exceptions,
        int wasCanceled,
        CancellationToken cancellationToken)
    {
        if (!exceptions.IsEmpty)
        {
            completionSource.TrySetException(exceptions);
        }
        else if (wasCanceled != 0)
        {
            var canceledToken = cancellationToken.IsCancellationRequested
                ? cancellationToken
                : new CancellationToken(canceled: true);

            completionSource.TrySetCanceled(canceledToken);
        }
        else
        {
            completionSource.TrySetResult();
        }
    }
}
