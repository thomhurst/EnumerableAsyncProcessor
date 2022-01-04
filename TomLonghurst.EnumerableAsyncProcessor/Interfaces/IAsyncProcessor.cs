namespace TomLonghurst.EnumerableAsyncProcessor.Interfaces;

public interface IAsyncProcessor<TResult>
{
    IEnumerable<Task<TResult>> GetEnumerableTasks();
    Task<IEnumerable<TResult>> GetResults();
    Task GetOverallProgressTask();
}