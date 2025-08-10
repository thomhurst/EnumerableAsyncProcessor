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
#if NET9_0_OR_GREATER
        await foreach (var task in Task.WhenEach(tasks).ConfigureAwait(false))
        {
            yield return task.Result;
        }
#else
        var managedTasksList = tasks.ToList();

        while (managedTasksList.Count != 0)
        {
            var finishedTask = await Task.WhenAny(managedTasksList).ConfigureAwait(false);
            managedTasksList.Remove(finishedTask);
            yield return await finishedTask.ConfigureAwait(false);
        }
#endif
    }

}