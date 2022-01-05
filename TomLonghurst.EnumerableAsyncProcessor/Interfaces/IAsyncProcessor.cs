﻿using System.Runtime.CompilerServices;

namespace TomLonghurst.EnumerableAsyncProcessor.Interfaces;

public interface IAsyncProcessor
{
 /**
     * <summary>
     * A collection of all the asynchronous Tasks, which could be pending or complete.
     * </summary>
     */
 IEnumerable<Task> GetEnumerableTasks();

 TaskAwaiter GetAwaiter();

 Task WaitAsync();
}