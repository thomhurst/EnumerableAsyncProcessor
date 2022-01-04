namespace TomLonghurst.EnumerableAsyncProcessor.Extensions;

internal static class TaskExtensions
{
    internal static bool TryStart(this Task task)
    {
        try
        {
            task.Start();
            return true;
        }
        catch
        {
            return false;
        }
    }
}