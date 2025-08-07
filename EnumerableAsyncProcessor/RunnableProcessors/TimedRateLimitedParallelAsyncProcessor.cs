using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.RunnableProcessors.Abstract;
using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors;

public class TimedRateLimitedParallelAsyncProcessor : AbstractAsyncProcessor
{
    private readonly int _levelsOfParallelism;
    private readonly TimeSpan _timeSpan;

    internal TimedRateLimitedParallelAsyncProcessor(int count, Func<Task> taskSelector, int levelsOfParallelism, TimeSpan timeSpan, CancellationTokenSource cancellationTokenSource) : base(count, taskSelector, cancellationTokenSource)
    {
        ValidationHelper.ValidateParallelism(levelsOfParallelism);
        ValidationHelper.ValidateTimeSpan(timeSpan);

        _levelsOfParallelism = levelsOfParallelism;
        _timeSpan = timeSpan;
    }

    internal override Task Process()
    {
        return TaskWrappers.InParallelAsync(_levelsOfParallelism, 
            async taskWrapper =>
            {
                await Task.WhenAll(
                    Task.Run(() => taskWrapper.Process(CancellationToken)),
                    Task.Delay(_timeSpan, CancellationToken));
            });
    }
}