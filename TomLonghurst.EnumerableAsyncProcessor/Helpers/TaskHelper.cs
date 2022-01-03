﻿namespace TomLonghurst.EnumerableAsyncProcessor.Helpers;

internal static class TaskHelper
{
    internal static List<Task<Task<TResult>>> CreateTasksWithoutStarting<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, Task<TResult>> taskSelector)
    {
        return source.Select(item => new Task<Task<TResult>>(() => taskSelector(item))).ToList();
    }

    internal static void StartAll<TResult>(IEnumerable<Task<Task<TResult>>> currentBatch)
    {
        foreach (var task in currentBatch)
        {
            task.Start();
        }
    }
}