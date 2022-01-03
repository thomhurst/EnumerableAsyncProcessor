namespace TomLonghurst.EnumerableAsyncProcessor.Interfaces;

public interface IRunnableAsyncRegulator<TResult>
{
    IEnumerable<Task<TResult>> GetEnumerableTasks();
    Task<IEnumerable<TResult>> GetResults();
    Task GetOverallProgressTask();
}