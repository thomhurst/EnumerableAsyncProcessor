#if NETSTANDARD2_0

namespace EnumerableAsyncProcessor
{
    /// <inheritdoc cref="TaskCompletionSource{TResult}"/>
    public class TaskCompletionSource : TaskCompletionSource<object?>
    {
        /// <inheritdoc cref="TaskCompletionSource{TResult}.TrySetResult"/>
        public bool TrySetResult()
        {
            return TrySetResult(null);
        }

        /// <inheritdoc cref="TaskCompletionSource{TResult}.SetResult"/>
        public void SetResult()
        {
            base.SetResult(null);
        }

        /// <inheritdoc cref="TaskCompletionSource{TResult}.Task"/>
        public new Task Task => base.Task;
    }
}
#endif