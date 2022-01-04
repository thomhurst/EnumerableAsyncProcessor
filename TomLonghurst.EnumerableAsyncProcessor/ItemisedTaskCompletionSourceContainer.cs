namespace TomLonghurst.EnumerableAsyncProcessor;

public record ItemisedTaskCompletionSourceContainer<TSource>(TSource Item, TaskCompletionSource TaskCompletionSource);
public record ItemisedTaskCompletionSourceContainer<TSource, TResult>(TSource Item, TaskCompletionSource<TResult> TaskCompletionSource);