#if NET6_0_OR_GREATER
namespace EnumerableAsyncProcessor.Extensions;

public interface IAsyncEnumerableProcessor
{
    Task ExecuteAsync();
}

public interface IAsyncEnumerableProcessor<TOutput>
{
    IAsyncEnumerable<TOutput> ExecuteAsync();
}
#endif