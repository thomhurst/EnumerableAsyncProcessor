namespace TomLonghurst.EnumerableAsyncProcessor.Builders;

public class ExecutionCountAsyncProcessorBuilder
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

    public ActionAsyncProcessorBuilder ForEachAsync(Func<Task> taskSelector)
    {
        return ForEachAsync(taskSelector, CancellationToken.None);
    }

    public ActionAsyncProcessorBuilder ForEachAsync(Func<Task> taskSelector, CancellationToken cancellationToken)
    {
        return new ActionAsyncProcessorBuilder(_count, taskSelector, cancellationToken);
    }
}