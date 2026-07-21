namespace EnumerableAsyncProcessor.Interfaces;

/// <summary>
/// A single-use processor for an <see cref="IAsyncEnumerable{T}"/> source that performs
/// an operation per item without returning results.
/// </summary>
public interface IAsyncEnumerableProcessor : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Processes the source. The processor is single-use; it disposes its internal
    /// resources when processing completes.
    /// </summary>
    /// <returns>
    /// A task that completes when processing finishes. When multiple items fail, awaiting the
    /// task throws the first failure while <c>Task.Exception.InnerExceptions</c> carries every failure.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when called a second time.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the processor was disposed before ever executing.</exception>
    Task ExecuteAsync();
}

/// <summary>
/// A single-use processor for an <see cref="IAsyncEnumerable{T}"/> source that streams
/// one result per item.
/// </summary>
public interface IAsyncEnumerableProcessor<out TOutput> : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Processes the source, streaming results as they become available. The processor is
    /// single-use; it disposes its internal resources when enumeration finishes.
    /// </summary>
    /// <remarks>
    /// Result ordering depends on the processor configuration: one-at-a-time, batch, and
    /// bounded parallel (<c>maxConcurrency</c> set) processors yield results in source order;
    /// unbounded parallel processors yield results in completion order.
    /// Abandoning the stream early (breaking out of enumeration) cancels the processor's
    /// remaining work.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when called a second time.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the processor was disposed before ever executing.</exception>
    IAsyncEnumerable<TOutput> ExecuteAsync();
}
