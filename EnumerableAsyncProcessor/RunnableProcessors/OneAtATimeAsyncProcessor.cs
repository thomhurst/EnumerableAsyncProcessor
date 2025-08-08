using EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class OneAtATimeAsyncProcessor : AbstractAsyncProcessor
{
    internal OneAtATimeAsyncProcessor(int count, Func<Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
    }

    internal override async Task Process()
    {
        foreach (var taskWrapper in TaskWrappers)
        {
            await taskWrapper.Process(CancellationToken).ConfigureAwait(false);
        }
    }
}