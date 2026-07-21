namespace EnumerableAsyncProcessor.Builders;

public sealed class ExecutionCountAsyncProcessorBuilder
{
    private readonly int _count;

    internal ExecutionCountAsyncProcessorBuilder(int count)
    {
        _count = count;
    }

    public ActionAsyncProcessorBuilder<TOutput> SelectAsync<TOutput>(Func<Task<TOutput>> taskSelector)
    {
        return SelectAsync(taskSelector, CancellationToken.None);
    }

    public ActionAsyncProcessorBuilder<TOutput> SelectAsync<TOutput>(Func<Task<TOutput>> taskSelector, CancellationToken cancellationToken)
    {
        return new ActionAsyncProcessorBuilder<TOutput>(_count, taskSelector, cancellationToken);
    }

    public ActionAsyncProcessorBuilder<TOutput> SelectAsync<TOutput>(Func<CancellationToken, Task<TOutput>> taskSelector)
    {
        return SelectAsync(taskSelector, CancellationToken.None);
    }

    public ActionAsyncProcessorBuilder<TOutput> SelectAsync<TOutput>(Func<CancellationToken, Task<TOutput>> taskSelector, CancellationToken cancellationToken)
    {
        return new ActionAsyncProcessorBuilder<TOutput>(_count, taskSelector, cancellationToken);
    }

    public ActionAsyncProcessorBuilder ForEachAsync(Func<Task> taskSelector)
    {
        return ForEachAsync(taskSelector, CancellationToken.None);
    }

    public ActionAsyncProcessorBuilder ForEachAsync(Func<Task> taskSelector, CancellationToken cancellationToken)
    {
        return new ActionAsyncProcessorBuilder(_count, taskSelector, cancellationToken);
    }

    public ActionAsyncProcessorBuilder ForEachAsync(Func<CancellationToken, Task> taskSelector)
    {
        return ForEachAsync(taskSelector, CancellationToken.None);
    }

    public ActionAsyncProcessorBuilder ForEachAsync(Func<CancellationToken, Task> taskSelector, CancellationToken cancellationToken)
    {
        return new ActionAsyncProcessorBuilder(_count, taskSelector, cancellationToken);
    }
}
