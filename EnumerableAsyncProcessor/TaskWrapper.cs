namespace EnumerableAsyncProcessor;

/// <summary>
/// Completes a completion source from a failed task-factory invocation, classifying the failure
/// by the state of the task rather than the caught exception.
/// </summary>
internal static class TaskCompletionSourceExtensions
{
    internal static void TrySetFromFault(this TaskCompletionSource taskCompletionSource, Task? task, Exception exception, CancellationToken cancellationToken)
    {
        if (task is { IsCanceled: true })
        {
            taskCompletionSource.TrySetCanceled(cancellationToken);
        }
        else if (task is { IsFaulted: true })
        {
            // Preserve every failure from the task, not just the first
            taskCompletionSource.TrySetException(task.Exception!.InnerExceptions);
        }
        else
        {
            taskCompletionSource.TrySetException(exception);
        }
    }

    internal static void TrySetFromFault<TOutput>(this TaskCompletionSource<TOutput> taskCompletionSource, Task? task, Exception exception, CancellationToken cancellationToken)
    {
        if (task is { IsCanceled: true })
        {
            taskCompletionSource.TrySetCanceled(cancellationToken);
        }
        else if (task is { IsFaulted: true })
        {
            // Preserve every failure from the task, not just the first
            taskCompletionSource.TrySetException(task.Exception!.InnerExceptions);
        }
        else
        {
            taskCompletionSource.TrySetException(exception);
        }
    }
}

/// <summary>
/// A struct wrapper pairing an action task factory with its completion source.
/// </summary>
public readonly struct ActionTaskWrapper
{
    public readonly Func<Task> TaskFactory;
    public readonly TaskCompletionSource TaskCompletionSource;

    public ActionTaskWrapper(Func<Task> taskFactory, TaskCompletionSource taskCompletionSource)
    {
        TaskFactory = taskFactory;
        TaskCompletionSource = taskCompletionSource;
    }

    public async Task Process(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource.TrySetCanceled(cancellationToken);
            return;
        }

        Task? task = null;
        try
        {
            task = TaskFactory.Invoke();
            await task.ConfigureAwait(false);
            TaskCompletionSource.TrySetResult();
        }
        catch (Exception e)
        {
            TaskCompletionSource.TrySetFromFault(task, e, cancellationToken);
        }
    }
}

/// <summary>
/// A struct wrapper pairing an input item and its task factory with a completion source.
/// </summary>
public readonly struct ItemTaskWrapper<TInput>
{
    public readonly TInput Input;
    public readonly Func<TInput, Task> TaskFactory;
    public readonly TaskCompletionSource TaskCompletionSource;

    public ItemTaskWrapper(TInput input, Func<TInput, Task> taskFactory, TaskCompletionSource taskCompletionSource)
    {
        Input = input;
        TaskFactory = taskFactory;
        TaskCompletionSource = taskCompletionSource;
    }

    public async Task Process(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource.TrySetCanceled(cancellationToken);
            return;
        }

        Task? task = null;
        try
        {
            task = TaskFactory.Invoke(Input);
            await task.ConfigureAwait(false);
            TaskCompletionSource.TrySetResult();
        }
        catch (Exception e)
        {
            TaskCompletionSource.TrySetFromFault(task, e, cancellationToken);
        }
    }
}

/// <summary>
/// A struct wrapper pairing an input item and its result-producing task factory with a completion source.
/// </summary>
public readonly struct ItemTaskWrapper<TInput, TOutput>
{
    public readonly TInput Input;
    public readonly Func<TInput, Task<TOutput>> TaskFactory;
    public readonly TaskCompletionSource<TOutput> TaskCompletionSource;

    public ItemTaskWrapper(TInput input, Func<TInput, Task<TOutput>> taskFactory, TaskCompletionSource<TOutput> taskCompletionSource)
    {
        Input = input;
        TaskFactory = taskFactory;
        TaskCompletionSource = taskCompletionSource;
    }

    public async Task Process(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource.TrySetCanceled(cancellationToken);
            return;
        }

        Task<TOutput>? task = null;
        try
        {
            task = TaskFactory.Invoke(Input);
            TaskCompletionSource.TrySetResult(await task.ConfigureAwait(false));
        }
        catch (Exception e)
        {
            TaskCompletionSource.TrySetFromFault(task, e, cancellationToken);
        }
    }
}

/// <summary>
/// A struct wrapper pairing a result-producing task factory with its completion source.
/// </summary>
public readonly struct ActionTaskWrapper<TOutput>
{
    public readonly Func<Task<TOutput>> TaskFactory;
    public readonly TaskCompletionSource<TOutput> TaskCompletionSource;

    public ActionTaskWrapper(Func<Task<TOutput>> taskFactory, TaskCompletionSource<TOutput> taskCompletionSource)
    {
        TaskFactory = taskFactory;
        TaskCompletionSource = taskCompletionSource;
    }

    public async Task Process(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource.TrySetCanceled(cancellationToken);
            return;
        }

        Task<TOutput>? task = null;
        try
        {
            task = TaskFactory.Invoke();
            TaskCompletionSource.TrySetResult(await task.ConfigureAwait(false));
        }
        catch (Exception e)
        {
            TaskCompletionSource.TrySetFromFault(task, e, cancellationToken);
        }
    }
}
