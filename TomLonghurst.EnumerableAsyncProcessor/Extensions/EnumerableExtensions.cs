using TomLonghurst.EnumerableAsyncProcessor.Builders;

namespace TomLonghurst.EnumerableAsyncProcessor.Extensions;

public static class EnumerableExtensions
{
    public static ItemAsyncProcessorBuilder<T> ToAsyncProcessorBuilder<T>(this IEnumerable<T> items)
    {
        return new ItemAsyncProcessorBuilder<T>(items);
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