using EnumerableAsyncProcessor.Builders;

namespace EnumerableAsyncProcessor.Extensions;

public static class EnumerableExtensions
{
    public static ItemAsyncProcessorBuilder<T> ToAsyncProcessorBuilder<T>(this IEnumerable<T> items)
    {
        return new ItemAsyncProcessorBuilder<T>(items);
    }
    
    public static ItemActionAsyncProcessorBuilder<T, TOutput> SelectAsync<T, TOutput>(this IEnumerable<T> items, Func<T, Task<TOutput>> taskSelector, CancellationToken cancellationToken = default)
    {
        return items.ToAsyncProcessorBuilder()
            .SelectAsync(taskSelector, cancellationToken);
    }
    
    public static ItemActionAsyncProcessorBuilder<T> ForEachAsync<T>(this IEnumerable<T> items, Func<T, Task> taskSelector, CancellationToken cancellationToken = default)
    {
        return items.ToAsyncProcessorBuilder()
            .ForEachAsync(taskSelector, cancellationToken);
    }

    internal static async IAsyncEnumerable<T> ToIAsyncEnumerable<T>(this IEnumerable<Task<T>> tasks)
    {
        var managedTasksList = tasks.ToList();

        while (managedTasksList.Any())
        {
            var finishedTask = await Task.WhenAny(managedTasksList);
            managedTasksList.Remove(finishedTask);
            yield return await finishedTask;
        }
    }
}