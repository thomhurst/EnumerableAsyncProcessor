#if NET6_0_OR_GREATER
using System.Threading.Channels;
#endif

namespace EnumerableAsyncProcessor.Extensions;

public static class ParallelExtensions
{
    public static async Task InParallelAsync<TSource, TResult>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task<TResult>> taskSelector)
    {
        await InParallelAsync(source, levelOfParallelism, taskSelector, CancellationToken.None, true).ConfigureAwait(false);
    }

    public static async Task InParallelAsync<TSource, TResult>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task<TResult>> taskSelector,
        CancellationToken cancellationToken)
    {
        await InParallelAsync(source, levelOfParallelism, taskSelector, cancellationToken, true).ConfigureAwait(false);
    }

    public static async Task InParallelAsync<TSource, TResult>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task<TResult>> taskSelector,
        CancellationToken cancellationToken,
        bool isIOBound)
    {
        if (levelOfParallelism <= 0)
        {
            // For I/O-bound tasks, allow much higher concurrency
            levelOfParallelism = isIOBound ? Math.Max(100, Environment.ProcessorCount * 10) : Environment.ProcessorCount;
        }
        
        // For high-concurrency I/O operations, use a channel-based approach to reduce contention
#if NET6_0_OR_GREATER
        if (isIOBound && levelOfParallelism > Environment.ProcessorCount * 4)
        {
            await ProcessWithChannelAsync(source, levelOfParallelism, taskSelector, cancellationToken).ConfigureAwait(false);
            return;
        }
#endif
        
        using var parallelLock = new SemaphoreSlim(initialCount:levelOfParallelism, maxCount:levelOfParallelism);

        await Task.WhenAll(source.Select(item => ProcessAsync(item, taskSelector, parallelLock, cancellationToken, isIOBound))).ConfigureAwait(false);
    }
    
    public static async Task InParallelAsync<TSource>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task> taskSelector)
    {
        await InParallelAsync(source, levelOfParallelism, taskSelector, CancellationToken.None, true).ConfigureAwait(false);
    }

    public static async Task InParallelAsync<TSource>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task> taskSelector,
        CancellationToken cancellationToken)
    {
        await InParallelAsync(source, levelOfParallelism, taskSelector, cancellationToken, true).ConfigureAwait(false);
    }

    public static async Task InParallelAsync<TSource>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task> taskSelector,
        CancellationToken cancellationToken,
        bool isIOBound)
    {
        if (levelOfParallelism <= 0)
        {
            // For I/O-bound tasks, allow much higher concurrency
            levelOfParallelism = isIOBound ? Math.Max(100, Environment.ProcessorCount * 10) : Environment.ProcessorCount;
        }
        
        // For high-concurrency I/O operations, use a channel-based approach to reduce contention
#if NET6_0_OR_GREATER
        if (isIOBound && levelOfParallelism > Environment.ProcessorCount * 4)
        {
            await ProcessWithChannelAsync(source, levelOfParallelism, taskSelector, cancellationToken).ConfigureAwait(false);
            return;
        }
#endif
        
        using var parallelLock = new SemaphoreSlim(initialCount:levelOfParallelism, maxCount:levelOfParallelism);

        await Task.WhenAll(source.Select(item => ProcessAsync(item, taskSelector, parallelLock, cancellationToken, isIOBound))).ConfigureAwait(false);
    }

    // Overloads for CPU-bound processing
    public static async Task InParallelAsync<TSource, TResult>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, TResult> taskSelector,
        CancellationToken cancellationToken = default)
    {
        await InParallelAsync(source, levelOfParallelism, item => Task.FromResult(taskSelector(item)), cancellationToken, false).ConfigureAwait(false);
    }

    public static async Task InParallelAsync<TSource>(
        this IEnumerable<TSource> source,
        int levelOfParallelism,
        Action<TSource> taskSelector,
        CancellationToken cancellationToken = default)
    {
        await InParallelAsync(source, levelOfParallelism, item => { taskSelector(item); return Task.CompletedTask; }, cancellationToken, false).ConfigureAwait(false);
    }

#if NET6_0_OR_GREATER
    private static async Task ProcessWithChannelAsync<TSource, TResult>(
        IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task<TResult>> taskSelector,
        CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<TSource>();
        var writer = channel.Writer;
        var reader = channel.Reader;

        // Start producer
        var producerTask = Task.Run(async () =>
        {
            try
            {
                foreach (var item in source)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    await writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                writer.Complete();
            }
        }, cancellationToken);

        // Start consumers
        var consumerTasks = Enumerable.Range(0, Math.Min(levelOfParallelism, Environment.ProcessorCount))
            .Select(_ => Task.Run(async () =>
            {
                await foreach (var item in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    var task = taskSelector(item);
                    if (task.IsCompleted)
                    {
                        // Fast-path for already completed tasks
                        var result = await task.ConfigureAwait(false);
                    }
                    else
                    {
                        await task.ConfigureAwait(false);
                    }
                }
            }, cancellationToken))
            .ToArray();

        await Task.WhenAll(consumerTasks.Concat(new[] { producerTask })).ConfigureAwait(false);
    }

    private static async Task ProcessWithChannelAsync<TSource>(
        IEnumerable<TSource> source,
        int levelOfParallelism,
        Func<TSource, Task> taskSelector,
        CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<TSource>();
        var writer = channel.Writer;
        var reader = channel.Reader;

        // Start producer
        var producerTask = Task.Run(async () =>
        {
            try
            {
                foreach (var item in source)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    await writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                writer.Complete();
            }
        }, cancellationToken);

        // Start consumers
        var consumerTasks = Enumerable.Range(0, Math.Min(levelOfParallelism, Environment.ProcessorCount))
            .Select(_ => Task.Run(async () =>
            {
                await foreach (var item in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    var task = taskSelector(item);
                    if (task.IsCompleted)
                    {
                        // Fast-path for already completed tasks
                        await task.ConfigureAwait(false);
                    }
                    else
                    {
                        await task.ConfigureAwait(false);
                    }
                }
            }, cancellationToken))
            .ToArray();

        await Task.WhenAll(consumerTasks.Concat(new[] { producerTask })).ConfigureAwait(false);
    }
#endif

    private static async Task<TResult> ProcessAsync<TSource, TResult>(
        TSource item,
        Func<TSource, Task<TResult>> taskSelector,
        SemaphoreSlim parallelLock,
        CancellationToken cancellationToken,
        bool isIOBound)
    {
        await parallelLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var task = taskSelector(item);
            
            // Fast-path optimization for already completed tasks
            if (task.IsCompleted)
            {
                if (task.IsFaulted)
                    throw task.Exception?.GetBaseException() ?? task.Exception!;
                if (task.IsCanceled)
                    throw new OperationCanceledException();
                return task.Result;
            }

            // For I/O-bound tasks, don't use Task.Run wrapper
            if (isIOBound)
            {
                return await task.ConfigureAwait(false);
            }
            
            // For CPU-bound tasks, use Task.Run to offload to ThreadPool
            return await Task.Run(async () => await task.ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            parallelLock.Release();
        }
    }
    
    private static async Task ProcessAsync<TSource>(
        TSource item,
        Func<TSource, Task> taskSelector,
        SemaphoreSlim parallelLock,
        CancellationToken cancellationToken,
        bool isIOBound)
    {
        await parallelLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var task = taskSelector(item);
            
            // Fast-path optimization for already completed tasks
            if (task.IsCompleted)
            {
                if (task.IsFaulted)
                    throw task.Exception?.GetBaseException() ?? task.Exception!;
                if (task.IsCanceled)
                    throw new OperationCanceledException();
                return;
            }

            // For I/O-bound tasks, don't use Task.Run wrapper
            if (isIOBound)
            {
                await task.ConfigureAwait(false);
            }
            else
            {
                // For CPU-bound tasks, use Task.Run to offload to ThreadPool
                await Task.Run(async () => await task.ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            parallelLock.Release();
        }
    }
}