using System.Runtime.CompilerServices;

namespace EnumerableAsyncProcessor.Interfaces;

/// <summary>
/// Represents an async processor that performs operations without returning results.
/// </summary>
/// <remarks>
/// This interface implements both IDisposable and IAsyncDisposable. Processors should be properly disposed
/// to ensure cleanup of internal resources and cancellation of running tasks.
/// Use 'await using var processor = ...' for automatic disposal, or call DisposeAsync() manually.
/// </remarks>
public interface IAsyncProcessor : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets a collection of all the asynchronous Tasks, which could be pending or complete.
    /// Tasks appear in source order.
    /// </summary>
    /// <returns>An enumerable of tasks that may be running, completed, or faulted.</returns>
    IEnumerable<Task> GetEnumerableTasks();

    /// <summary>
    /// Gets a task awaiter for awaiting completion of all operations directly.
    /// </summary>
    /// <returns>A task awaiter that completes when all processing is done.</returns>
    TaskAwaiter GetAwaiter();

    /// <summary>
    /// Gets a task that completes when all operations have finished.
    /// </summary>
    /// <returns>
    /// A task that completes when all processing is done. When multiple items fail, awaiting it
    /// throws the first failure while <c>Task.Exception.InnerExceptions</c> carries every failure.
    /// </returns>
    Task WaitAsync();

    /// <summary>
    /// Attempts to cancel all unfinished tasks.
    /// </summary>
    /// <remarks>
    /// This method is called automatically during disposal but can be called manually
    /// if you need to cancel processing before disposal.
    /// </remarks>
    void CancelAll();
}
