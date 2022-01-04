﻿namespace TomLonghurst.EnumerableAsyncProcessor.Helpers;

internal static class TaskHelper
{
    internal static List<Task<Task<TResult>>> CreateTasksWithoutStarting<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, Task<TResult>> taskSelector, CancellationToken cancellationToken)
    {
        return source.Select(item => new Task<Task<TResult>>(() => taskSelector(item), cancellationToken)).ToList();
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

    internal static void StartAll(IEnumerable<Task<Task>> currentBatch)
    {
        foreach (var task in currentBatch)
        {
            task.Start();
        }
    }
}