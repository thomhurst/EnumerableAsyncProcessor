using EnumerableAsyncProcessor.RunnableProcessors.Abstract;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class OneAtATimeAsyncProcessor<TInput> : AbstractAsyncProcessor<TInput>
{
    internal OneAtATimeAsyncProcessor(IEnumerable<TInput> items, Func<TInput, Task> taskSelector, CancellationTokenSource cancellationTokenSource) : base(items, taskSelector, cancellationTokenSource)
    {
    }

    internal override async Task Process()
    {
        foreach (var taskWrapper in TaskWrappers)
        {
            await taskWrapper.Process(CancellationToken);
        }
    }
}