#if NET6_0_OR_GREATER
using EnumerableAsyncProcessor.Extensions;

namespace EnumerableAsyncProcessor.RunnableProcessors.AsyncEnumerable.ResultProcessors;

/// <summary>
/// Sequential processor that processes items one at a time and returns results in order.
/// </summary>
public class ResultAsyncEnumerableOneAtATimeProcessor<TInput, TOutput> : IAsyncEnumerableProcessor<TOutput>
{
    private readonly IAsyncEnumerable<TInput> _items;
    private readonly Func<TInput, Task<TOutput>> _taskSelector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    internal ResultAsyncEnumerableOneAtATimeProcessor(
        IAsyncEnumerable<TInput> items,
        Func<TInput, Task<TOutput>> taskSelector,
        CancellationTokenSource cancellationTokenSource)
    {
        _items = items;
        _taskSelector = taskSelector;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public async IAsyncEnumerable<TOutput> ExecuteAsync()
    {
        var cancellationToken = _cancellationTokenSource.Token;

        await foreach (var item in _items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var result = await _taskSelector(item).ConfigureAwait(false);
            yield return result;
        }
    }
}
#endif