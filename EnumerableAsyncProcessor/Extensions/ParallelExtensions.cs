using System.Collections.Concurrent;

namespace EnumerableAsyncProcessor.Extensions;

public static class ParallelExtensions
{
    public static Task InParallelAsync<TSource, TResult>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task<TResult>> taskSelector)
    {
        return InParallelAsync(source, levelOfParallelism, taskSelector, CancellationToken.None);
    }

    public static Task InParallelAsync<TSource, TResult>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task<TResult>> taskSelector,
        CancellationToken cancellationToken)
    {
        return StartWorkers(source, levelOfParallelism, taskSelector, cancellationToken);
    }

    public static Task InParallelAsync<TSource>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task> taskSelector)
    {
        return InParallelAsync(source, levelOfParallelism, taskSelector, CancellationToken.None);
    }

    public static Task InParallelAsync<TSource>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task> taskSelector,
        CancellationToken cancellationToken)
    {
        return StartWorkers(source, levelOfParallelism, taskSelector, cancellationToken);
    }

    // Overloads for CPU-bound processing
    public static Task InParallelAsync<TSource, TResult>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, TResult> taskSelector,
        CancellationToken cancellationToken = default)
    {
        return StartWorkers(
            source,
            levelOfParallelism,
            item =>
            {
                _ = taskSelector(item);
                return Task.CompletedTask;
            },
            cancellationToken);
    }

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
