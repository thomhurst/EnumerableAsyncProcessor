using System.Runtime.CompilerServices;

namespace EnumerableAsyncProcessor.Interfaces;

public interface IAsyncProcessor<TOutput>
{
    /**
     * <summary>
     * A collection of all the asynchronous Tasks, which could be pending or complete.
     * </summary>
     */
    IEnumerable<Task<TOutput>> GetEnumerableTasks();
    
    /**
     * <summary>
     * A task that will contain the mapped results when complete
     * </summary>
     */
    Task<TOutput[]> GetResultsAsync();

    IAsyncEnumerable<TOutput> GetResultsAsyncEnumerable();

    TaskAwaiter<TOutput[]> GetAwaiter();
    
    /**
  * <summary>
  * Try to cancel all un-finished tasks
  * </summary>
  */
    void CancelAll();
}