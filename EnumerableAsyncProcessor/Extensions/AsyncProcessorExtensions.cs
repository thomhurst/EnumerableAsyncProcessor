using EnumerableAsyncProcessor.Interfaces;
using EnumerableAsyncProcessor.RunnableProcessors.Abstract;
using EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

namespace EnumerableAsyncProcessor.Extensions;

internal static class AsyncProcessorExtensions
{
    internal static IAsyncProcessor StartProcessing(this AbstractAsyncProcessorBase processor)
    {
        processor.Start();
        return processor;
    }

    internal static IAsyncProcessor<T1> StartProcessing<T1>(this ResultAbstractAsyncProcessorBase<T1> processor)
    {
        processor.Start();
        return processor;
    }
}
