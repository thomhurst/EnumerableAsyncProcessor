namespace EnumerableAsyncProcessor;

/// <summary>
/// Shared plumbing for the single-use IAsyncEnumerable processors: the single-use execution
/// guard, and exception collection that preserves every failure from a task the way the
/// enumerable processors' completion sources do.
/// </summary>
internal static class StreamingExecution
{
    /// <summary>
    /// Marks the processor as executed. A second call throws <see cref="InvalidOperationException"/>;
    /// a first call on a disposed processor throws <see cref="ObjectDisposedException"/>.
    /// </summary>
    public static void GuardSingleUse(ref int executionState, int disposed, object processor)
    {
        if (Interlocked.Exchange(ref executionState, 1) != 0)
        {
            throw new InvalidOperationException(
                $"{processor.GetType().Name} is single-use; ExecuteAsync may only be called once.");
        }

        if (disposed != 0)
        {
            throw new ObjectDisposedException(processor.GetType().Name);
        }
    }

    /// <summary>
    /// Records the failure(s) behind an awaited task, classifying by task state so a
    /// multi-fault task contributes every inner exception rather than only the first one
    /// the await rethrew.
    /// </summary>
    public static void CollectFailures(Task task, Exception thrown, List<Exception> exceptions, ref bool wasCanceled)
    {
        if (task.IsFaulted)
        {
            exceptions.AddRange(task.Exception!.InnerExceptions);
        }
        else if (thrown is OperationCanceledException)
        {
            wasCanceled = true;
        }
        else
        {
            exceptions.Add(thrown);
        }
    }

    /// <summary>
    /// Completes the execution task with Task.WhenAll fidelity: awaiting it throws the first
    /// exception while <c>Task.Exception.InnerExceptions</c> carries the full set.
    /// </summary>
    public static void Complete(TaskCompletionSource completionSource, List<Exception> exceptions, bool wasCanceled, CancellationToken cancellationToken)
    {
        if (exceptions.Count > 0)
        {
            completionSource.TrySetException(exceptions);
        }
        else if (wasCanceled)
        {
            completionSource.TrySetCanceled(
                cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(canceled: true));
        }
        else
        {
            completionSource.TrySetResult();
        }
    }

    /// <summary>
    /// Observes the exception of every task, attaching a continuation to those still running,
    /// so abandoned work can never surface as <c>TaskScheduler.UnobservedTaskException</c>.
    /// </summary>
    public static void ObserveFailures(IEnumerable<Task> tasks)
    {
        foreach (var task in tasks)
        {
            if (task.IsCompleted)
            {
                _ = task.Exception;
            }
            else
            {
                _ = task.ContinueWith(
                    static t => _ = t.Exception,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }
    }
}
