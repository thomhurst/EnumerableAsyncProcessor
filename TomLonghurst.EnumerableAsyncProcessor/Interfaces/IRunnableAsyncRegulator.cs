namespace TomLonghurst.EnumerableAsyncProcessor.Interfaces;

public interface IRunnableAsyncRegulator<TResult>
{
    IEnumerable<Task<TResult>> GetInnerTasks();
    Task<IEnumerable<TResult>> GetResults();
    Task GetTotalProgressTask();
}