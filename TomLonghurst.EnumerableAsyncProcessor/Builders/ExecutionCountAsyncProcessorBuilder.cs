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
        return new ActionAsyncProcessorBuilder<TResult>(_count, taskSelector);
    }
    
    public ActionAsyncProcessorBuilder ForEachAsync(Func<Task> taskSelector)
    {
        return new ActionAsyncProcessorBuilder(_count, taskSelector);
    }
}