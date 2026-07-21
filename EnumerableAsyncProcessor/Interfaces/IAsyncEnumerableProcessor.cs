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
    IAsyncEnumerable<TOutput> ExecuteAsync();
}
