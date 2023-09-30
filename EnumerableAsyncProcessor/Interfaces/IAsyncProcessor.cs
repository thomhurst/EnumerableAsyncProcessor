using System.Runtime.CompilerServices;

namespace EnumerableAsyncProcessor.Interfaces;

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

 /**
  * <summary>
  * Try to cancel all un-finished tasks
  * </summary>
  */
 void CancelAll();
}