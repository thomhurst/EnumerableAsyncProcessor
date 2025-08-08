#if NET6_0_OR_GREATER
namespace EnumerableAsyncProcessor;

public class AsyncEnumerableChannelOptions
{
    /// <summary>
    /// Buffer size for the channel. If null, uses unbounded channel.
    /// </summary>
    public int? BufferSize { get; set; }
    
    /// <summary>
    /// Number of concurrent consumers processing items from the channel.
    /// </summary>
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    
    /// <summary>
    /// Whether to preserve the order of results (for SelectAsync operations).
    /// </summary>
    public bool PreserveOrder { get; set; } = false;
    
    /// <summary>
    /// Whether tasks are I/O-bound (true) or CPU-bound (false).
    /// </summary>
    public bool IsIOBound { get; set; } = true;
}
#endif