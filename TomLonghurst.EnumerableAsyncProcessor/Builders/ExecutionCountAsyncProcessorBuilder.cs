namespace TomLonghurst.EnumerableAsyncProcessor.Builders;

public class ExecutionCountAsyncProcessorBuilder
{
    private readonly int _count;

    internal ExecutionCountAsyncProcessorBuilder(int count)
    {
        _count = count;
    }

    public ActionAsyncProcessorBuilder<TResult> SelectAsync<TResult>(Func<Task<TResult>> taskSelector)
    {
        return SelectAsync(taskSelector, CancellationToken.None);
    }

    public ActionAsyncProcessorBuilder<TResult> SelectAsync<TResult>(Func<Task<TResult>> taskSelector, CancellationToken cancellationToken)
    {
        return new ActionAsyncProcessorBuilder<TResult>(_count, taskSelector, cancellationToken);
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