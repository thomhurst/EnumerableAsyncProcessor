#if NET6_0_OR_GREATER
using System.Threading.Channels;

namespace EnumerableAsyncProcessor;

/// <summary>
/// Configuration options for channel-based processing.
/// </summary>
public class ChannelProcessorOptions
{
    /// <summary>
    /// The capacity of the channel. If null, an unbounded channel will be used.
    /// </summary>
    public int? Capacity { get; set; }

    /// <summary>
    /// The behavior when the channel is full (bounded channels only).
    /// </summary>
    public BoundedChannelFullMode FullMode { get; set; } = BoundedChannelFullMode.Wait;

    /// <summary>
    /// Whether to allow synchronous continuations on the channel.
    /// </summary>
    public bool AllowSynchronousContinuations { get; set; } = false;

    /// <summary>
    /// Whether to use a single reader optimization.
    /// </summary>
    public bool SingleReader { get; set; } = false;

    /// <summary>
    /// Whether to use a single writer optimization.
    /// </summary>
    public bool SingleWriter { get; set; } = true;

    /// <summary>
    /// The number of concurrent consumers that will process items from the channel.
    /// </summary>
    public int ConsumerCount { get; set; } = 1;

    /// <summary>
    /// Creates default options for bounded channels.
    /// </summary>
    /// <param name="capacity">The capacity of the bounded channel.</param>
    /// <param name="consumerCount">The number of concurrent consumers.</param>
    /// <returns>A new ChannelProcessorOptions instance.</returns>
    public static ChannelProcessorOptions CreateBounded(int capacity, int consumerCount = 1)
    {
        return new ChannelProcessorOptions
        {
            Capacity = capacity,
            ConsumerCount = consumerCount,
            FullMode = BoundedChannelFullMode.Wait
        };
    }

    /// <summary>
    /// Creates default options for unbounded channels.
    /// </summary>
    /// <param name="consumerCount">The number of concurrent consumers.</param>
    /// <returns>A new ChannelProcessorOptions instance.</returns>
    public static ChannelProcessorOptions CreateUnbounded(int consumerCount = 1)
    {
        return new ChannelProcessorOptions
        {
            Capacity = null,
            ConsumerCount = consumerCount
        };
    }

    /// <summary>
    /// Creates channel options based on the configuration.
    /// </summary>
    /// <returns>Channel options for creating a channel.</returns>
    internal UnboundedChannelOptions CreateUnboundedChannelOptions()
    {
        return new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = AllowSynchronousContinuations,
            SingleReader = SingleReader,
            SingleWriter = SingleWriter
        };
    }

    /// <summary>
    /// Creates channel options based on the configuration.
    /// </summary>
    /// <returns>Channel options for creating a bounded channel.</returns>
    internal BoundedChannelOptions CreateBoundedChannelOptions()
    {
        if (!Capacity.HasValue)
            throw new InvalidOperationException("Capacity must be set for bounded channels.");

        return new BoundedChannelOptions(Capacity.Value)
        {
            AllowSynchronousContinuations = AllowSynchronousContinuations,
            SingleReader = SingleReader,
            SingleWriter = SingleWriter,
            FullMode = FullMode
        };
    }
}
#endif