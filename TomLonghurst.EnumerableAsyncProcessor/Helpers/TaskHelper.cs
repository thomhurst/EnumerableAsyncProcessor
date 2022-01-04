namespace TomLonghurst.EnumerableAsyncProcessor.Helpers;

internal static class TaskHelper
{
    internal static List<Task<Task<TResult>>> CreateTasksWithoutStarting<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, Task<TResult>> taskSelector, CancellationToken cancellationToken)
    {
        return source.Select(item => new Task<Task<TResult>>(() => taskSelector(item), cancellationToken)).ToList();
    }
    
    internal static List<Task<Task<TResult>>> CreateTasksWithoutStarting<TResult>(int count, Func<Task<TResult>> taskSelector, CancellationToken cancellationToken)
    {
        return Enumerable.Range(0, count).Select(item => new Task<Task<TResult>>(taskSelector, cancellationToken)).ToList();
    }

    internal static void StartAll<TResult>(IEnumerable<Task<Task<TResult>>> currentBatch)
    {
        foreach (var task in currentBatch)
        {
            task.Start();
        }
    }
    
    internal static List<Task<Task>> CreateTasksWithoutStarting<TSource>(IEnumerable<TSource> source, Func<TSource, Task> taskSelector, CancellationToken cancellationToken)
    {
        return source.Select(item => new Task<Task>(() => taskSelector(item), cancellationToken)).ToList();
    }
    
    internal static List<Task<Task>> CreateTasksWithoutStarting(int count, Func<Task> taskSelector, CancellationToken cancellationToken)
    {
        return Enumerable.Range(0, count).Select(item => new Task<Task>(taskSelector, cancellationToken)).ToList();
    }

    internal static void StartAll(IEnumerable<Task<Task>> currentBatch)
    {
        foreach (var task in currentBatch)
        {
            task.Start();
        }
    }
}