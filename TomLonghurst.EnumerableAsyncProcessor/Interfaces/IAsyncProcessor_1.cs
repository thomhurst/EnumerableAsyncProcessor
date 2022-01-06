using System.Runtime.CompilerServices;

namespace TomLonghurst.EnumerableAsyncProcessor.Interfaces;

public interface IAsyncProcessor<TResult>
{
    /**
     * <summary>
     * A collection of all the asynchronous Tasks, which could be pending or complete.
     * </summary>
     */
    IEnumerable<Task<TResult>> GetEnumerableTasks();
    
    /**
     * <summary>
     * A task that will contain the mapped results when complete
     * </summary>
     */
    Task<TResult[]> GetResultsAsync();

    IAsyncEnumerable<TResult> GetResultsAsyncEnumerable();

    TaskAwaiter<TResult[]> GetAwaiter();
    
    /**
  * <summary>
  * Try to cancel all un-finished tasks
  * </summary>
  */
    void CancelAll();
}