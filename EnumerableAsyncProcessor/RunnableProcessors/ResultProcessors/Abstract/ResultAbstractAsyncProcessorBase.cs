using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EnumerableAsyncProcessor.Interfaces;
using EnumerableAsyncProcessor.Extensions;
using EnumerableAsyncProcessor.Validation;

namespace EnumerableAsyncProcessor.RunnableProcessors.ResultProcessors.Abstract;

public abstract class ResultAbstractAsyncProcessorBase<TOutput> : IAsyncProcessor<TOutput>, IAsyncDisposable, IDisposable
{
    protected abstract IEnumerable<TaskCompletionSource<TOutput>> EnumerableTaskCompletionSources { get; }
    protected readonly CancellationToken CancellationToken;

    [field: MaybeNull, AllowNull]
    private IEnumerable<Task<TOutput>> EnumerableTasks => field ??= EnumerableTaskCompletionSources.Select(x => x.Task);
    
    private readonly CancellationTokenSource _cancellationTokenSource;
    private volatile bool _disposed;
    private readonly object _disposeLock = new();
    
    [field: AllowNull, MaybeNull]
    private Task<TOutput[]> Results  => field ??= Task.WhenAll(EnumerableTasks);


    protected ResultAbstractAsyncProcessorBase(CancellationTokenSource cancellationTokenSource)
    {
        ValidationHelper.ValidateCancellationTokenSource(cancellationTokenSource);
        
        _cancellationTokenSource = cancellationTokenSource;
        
        CancellationToken = cancellationTokenSource.Token;
        CancellationToken.Register(Dispose);
        CancellationToken.ThrowIfCancellationRequested();
    }

    internal abstract Task Process();
    
    public IEnumerable<Task<TOutput>> GetEnumerableTasks()
    {
        return EnumerableTasks;
    }

    public Task<TOutput[]> GetResultsAsync()
    {
        return Results;
    }

    public IAsyncEnumerable<TOutput> GetResultsAsyncEnumerable()
    {
        return GetEnumerableTasks().ToIAsyncEnumerable();
    }

    public TaskAwaiter<TOutput[]> GetAwaiter()
    {
        return GetResultsAsync().GetAwaiter();
    }

    public void CancelAll()
    {
        if (_disposed)
            return;
            
        if (!_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
        }

        foreach (var taskCompletionSource in EnumerableTaskCompletionSources)
        {
            taskCompletionSource.TrySetCanceled(CancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        lock (_disposeLock)
        {
            if (_disposed)
                return;
            _disposed = true;
        }

        // Allow derived classes to dispose their resources first
        await DisposeAsyncCore().ConfigureAwait(false);

        // Cancel all operations
        CancelAll();

        // Wait for all running tasks to complete with timeout
        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var allTasks = EnumerableTasks.ToList();
            
            if (allTasks.Count > 0)
            {
                var completionTasks = allTasks.Select(async task =>
                {
                    try
                    {
                        await task.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancelled - ignore
                    }
                    catch (Exception)
                    {
                        // Task exceptions are expected - ignore during disposal
                    }
                }).ToList();

                if (completionTasks.Count > 0)
                {
                    try
                    {
                        await Task.WhenAll(completionTasks).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Timeout occurred - continue with disposal
                    }
                }
            }
        }
        catch (Exception)
        {
            // Swallow exceptions during disposal cleanup
        }

        // Dispose the cancellation token source
        try
        {
            _cancellationTokenSource.Dispose();
        }
        catch (Exception)
        {
            // Swallow disposal exceptions
        }

        GC.SuppressFinalize(this);
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
#if NET6_0_OR_GREATER
        return ValueTask.CompletedTask;
#else
        return new ValueTask(Task.CompletedTask);
#endif
    }

    public void Dispose()
    {
        // Synchronous disposal calls async disposal and blocks
        DisposeAsync().GetAwaiter().GetResult();
    }
}