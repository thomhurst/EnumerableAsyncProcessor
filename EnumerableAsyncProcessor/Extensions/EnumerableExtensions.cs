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

    internal static async IAsyncEnumerable<T> ToIAsyncEnumerable<T>(this IEnumerable<Task<T>> tasks, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
#if NET9_0_OR_GREATER
        await foreach (var task in Task.WhenEach(tasks).ConfigureAwait(false).WithCancellation(cancellationToken))
        {
            yield return task.Result;
        }
#else
        var managedTasksList = tasks.ToList();

        // Create a cancellation task that will complete when cancellation is requested
        using var cancellationTcs = new CancellationTokenSource();
        var cancellationTask = Task.Delay(Timeout.Infinite, cancellationTcs.Token);
        
        // Register callback to trigger the cancellation task
        using var registration = cancellationToken.Register(() => cancellationTcs.Cancel());

        while (managedTasksList.Count != 0)
        {
            // Check for cancellation before each iteration
            cancellationToken.ThrowIfCancellationRequested();
            
            // Include the cancellation task in WhenAny
            var allTasks = new List<Task>(managedTasksList.Count + 1);
            allTasks.AddRange(managedTasksList);
            allTasks.Add(cancellationTask);
            
            var finishedTask = await Task.WhenAny(allTasks).ConfigureAwait(false);
            
            // If the cancellation task completed, throw cancellation
            if (finishedTask == cancellationTask)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // This should not happen as cancellation should throw above, but as a safety measure:
                throw new OperationCanceledException(cancellationToken);
            }
            
            // Remove and yield the completed task
            var completedTask = (Task<T>)finishedTask;
            managedTasksList.Remove(completedTask);
            yield return await completedTask.ConfigureAwait(false);
        }
#endif
    }

}