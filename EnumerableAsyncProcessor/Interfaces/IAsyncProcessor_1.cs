using System.Runtime.CompilerServices;

namespace EnumerableAsyncProcessor.Interfaces;

/// <summary>
/// Represents an async processor that produces results and implements disposable patterns.
/// </summary>
/// <typeparam name="TOutput">The type of output produced by the processor.</typeparam>
/// <remarks>
/// This interface implements both IDisposable and IAsyncDisposable. Processors should be properly disposed 
/// to ensure cleanup of internal resources and cancellation of running tasks.
/// Use 'await using var processor = ...' for automatic disposal, or call DisposeAsync() manually.
/// </remarks>
public interface IAsyncProcessor<TOutput> : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets a collection of all the asynchronous Tasks, which could be pending or complete.
    /// </summary>
    /// <returns>An enumerable of tasks that may be running, completed, or faulted.</returns>
    IEnumerable<Task<TOutput>> GetEnumerableTasks();
    
    /// <summary>
    /// Gets a task that will contain all mapped results when complete.
    /// </summary>
    /// <returns>A task that completes when all processing is done, containing an array of results.</returns>
    Task<TOutput[]> GetResultsAsync();

    /// <summary>
    /// Gets results as an async enumerable that yields items as they complete.
    /// </summary>
    /// <returns>An async enumerable that yields results as they become available.</returns>
    /// <remarks>
    /// This method allows you to process results as they complete rather than waiting for all tasks to finish.
    /// The processor should still be disposed after consuming the async enumerable.
    /// </remarks>
    IAsyncEnumerable<TOutput> GetResultsAsyncEnumerable();

    /// <summary>
    /// Gets a task awaiter for awaiting all results directly.
    /// </summary>
    /// <returns>A task awaiter for the results array.</returns>
    TaskAwaiter<TOutput[]> GetAwaiter();
    
    /// <summary>
    /// Attempts to cancel all unfinished tasks.
    /// </summary>
    /// <remarks>
    /// This method is called automatically during disposal but can be called manually 
    /// if you need to cancel processing before disposal.
    /// </remarks>
    void CancelAll();
}