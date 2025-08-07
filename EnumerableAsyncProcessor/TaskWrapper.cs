using System.Runtime.CompilerServices;

namespace EnumerableAsyncProcessor;

/// <summary>
/// A high-performance struct wrapper for action tasks to reduce heap allocations.
/// </summary>
public readonly struct ActionTaskWrapper : IEquatable<ActionTaskWrapper>
{
    public readonly Func<Task> TaskFactory;
    public readonly TaskCompletionSource TaskCompletionSource;

    public ActionTaskWrapper(Func<Task> taskFactory, TaskCompletionSource taskCompletionSource)
    {
        TaskFactory = taskFactory;
        TaskCompletionSource = taskCompletionSource;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task Process(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource.SetCanceled(cancellationToken);
            return;
        }
        
        try
        {
            await TaskFactory.Invoke();
            TaskCompletionSource.SetResult();
        }
        catch (Exception e)
        {
            TaskCompletionSource.SetException(e);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ActionTaskWrapper other) =>
        ReferenceEquals(TaskFactory, other.TaskFactory) &&
        ReferenceEquals(TaskCompletionSource, other.TaskCompletionSource);

    public override bool Equals(object? obj) =>
        obj is ActionTaskWrapper other && Equals(other);

    public override int GetHashCode()
    {
#if NETSTANDARD2_0
        unchecked
        {
            var hash = 17;
            hash = hash * 23 + (TaskFactory?.GetHashCode() ?? 0);
            hash = hash * 23 + (TaskCompletionSource?.GetHashCode() ?? 0);
            return hash;
        }
#else
        return HashCode.Combine(TaskFactory, TaskCompletionSource);
#endif
    }

    public static bool operator ==(ActionTaskWrapper left, ActionTaskWrapper right) =>
        left.Equals(right);

    public static bool operator !=(ActionTaskWrapper left, ActionTaskWrapper right) =>
        !left.Equals(right);

#if NET6_0_OR_GREATER
    public void Deconstruct(out Func<Task> taskFactory, out TaskCompletionSource taskCompletionSource)
    {
        taskFactory = TaskFactory;
        taskCompletionSource = TaskCompletionSource;
    }
#endif
}

/// <summary>
/// A high-performance struct wrapper for item tasks to reduce heap allocations.
/// </summary>
public readonly struct ItemTaskWrapper<TInput> : IEquatable<ItemTaskWrapper<TInput>>
{
    public readonly TInput Input;
    public readonly Func<TInput, Task> TaskFactory;
    public readonly TaskCompletionSource TaskCompletionSource;

    public ItemTaskWrapper(TInput input, Func<TInput, Task> taskFactory, TaskCompletionSource taskCompletionSource)
    {
        Input = input;
        TaskFactory = taskFactory;
        TaskCompletionSource = taskCompletionSource;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task Process(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource.SetCanceled(cancellationToken);
            return;
        }
        
        try
        {
            await TaskFactory.Invoke(Input);
            TaskCompletionSource.SetResult();
        }
        catch (Exception e)
        {
            TaskCompletionSource.SetException(e);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ItemTaskWrapper<TInput> other) =>
        EqualityComparer<TInput>.Default.Equals(Input, other.Input) &&
        ReferenceEquals(TaskFactory, other.TaskFactory) &&
        ReferenceEquals(TaskCompletionSource, other.TaskCompletionSource);

    public override bool Equals(object? obj) =>
        obj is ItemTaskWrapper<TInput> other && Equals(other);

    public override int GetHashCode()
    {
#if NETSTANDARD2_0
        unchecked
        {
            var hash = 17;
            hash = hash * 23 + (Input?.GetHashCode() ?? 0);
            hash = hash * 23 + (TaskFactory?.GetHashCode() ?? 0);
            hash = hash * 23 + (TaskCompletionSource?.GetHashCode() ?? 0);
            return hash;
        }
#else
        return HashCode.Combine(Input, TaskFactory, TaskCompletionSource);
#endif
    }

    public static bool operator ==(ItemTaskWrapper<TInput> left, ItemTaskWrapper<TInput> right) =>
        left.Equals(right);

    public static bool operator !=(ItemTaskWrapper<TInput> left, ItemTaskWrapper<TInput> right) =>
        !left.Equals(right);

#if NET6_0_OR_GREATER
    public void Deconstruct(out TInput input, out Func<TInput, Task> taskFactory, out TaskCompletionSource taskCompletionSource)
    {
        input = Input;
        taskFactory = TaskFactory;
        taskCompletionSource = TaskCompletionSource;
    }
#endif
}

/// <summary>
/// A high-performance struct wrapper for item tasks with results to reduce heap allocations.
/// </summary>
public readonly struct ItemTaskWrapper<TInput, TOutput> : IEquatable<ItemTaskWrapper<TInput, TOutput>>
{
    public readonly TInput Input;
    public readonly Func<TInput, Task<TOutput>> TaskFactory;
    public readonly TaskCompletionSource<TOutput> TaskCompletionSource;

    public ItemTaskWrapper(TInput input, Func<TInput, Task<TOutput>> taskFactory, TaskCompletionSource<TOutput> taskCompletionSource)
    {
        Input = input;
        TaskFactory = taskFactory;
        TaskCompletionSource = taskCompletionSource;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task Process(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource.SetCanceled(cancellationToken);
            return;
        }
        
        try
        {
            TaskCompletionSource.SetResult(await TaskFactory.Invoke(Input));
        }
        catch (Exception e)
        {
            TaskCompletionSource.SetException(e);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ItemTaskWrapper<TInput, TOutput> other) =>
        EqualityComparer<TInput>.Default.Equals(Input, other.Input) &&
        ReferenceEquals(TaskFactory, other.TaskFactory) &&
        ReferenceEquals(TaskCompletionSource, other.TaskCompletionSource);

    public override bool Equals(object? obj) =>
        obj is ItemTaskWrapper<TInput, TOutput> other && Equals(other);

    public override int GetHashCode()
    {
#if NETSTANDARD2_0
        unchecked
        {
            var hash = 17;
            hash = hash * 23 + (Input?.GetHashCode() ?? 0);
            hash = hash * 23 + (TaskFactory?.GetHashCode() ?? 0);
            hash = hash * 23 + (TaskCompletionSource?.GetHashCode() ?? 0);
            return hash;
        }
#else
        return HashCode.Combine(Input, TaskFactory, TaskCompletionSource);
#endif
    }

    public static bool operator ==(ItemTaskWrapper<TInput, TOutput> left, ItemTaskWrapper<TInput, TOutput> right) =>
        left.Equals(right);

    public static bool operator !=(ItemTaskWrapper<TInput, TOutput> left, ItemTaskWrapper<TInput, TOutput> right) =>
        !left.Equals(right);

#if NET6_0_OR_GREATER
    public void Deconstruct(out TInput input, out Func<TInput, Task<TOutput>> taskFactory, out TaskCompletionSource<TOutput> taskCompletionSource)
    {
        input = Input;
        taskFactory = TaskFactory;
        taskCompletionSource = TaskCompletionSource;
    }
#endif
}

/// <summary>
/// A high-performance struct wrapper for action tasks with results to reduce heap allocations.
/// </summary>
public readonly struct ActionTaskWrapper<TOutput> : IEquatable<ActionTaskWrapper<TOutput>>
{
    public readonly Func<Task<TOutput>> TaskFactory;
    public readonly TaskCompletionSource<TOutput> TaskCompletionSource;

    public ActionTaskWrapper(Func<Task<TOutput>> taskFactory, TaskCompletionSource<TOutput> taskCompletionSource)
    {
        TaskFactory = taskFactory;
        TaskCompletionSource = taskCompletionSource;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task Process(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource.SetCanceled(cancellationToken);
            return;
        }
        
        try
        { 
            TaskCompletionSource.SetResult(await TaskFactory.Invoke());
        }
        catch (Exception e)
        {
            TaskCompletionSource.SetException(e);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ActionTaskWrapper<TOutput> other) =>
        ReferenceEquals(TaskFactory, other.TaskFactory) &&
        ReferenceEquals(TaskCompletionSource, other.TaskCompletionSource);

    public override bool Equals(object? obj) =>
        obj is ActionTaskWrapper<TOutput> other && Equals(other);

    public override int GetHashCode()
    {
#if NETSTANDARD2_0
        unchecked
        {
            var hash = 17;
            hash = hash * 23 + (TaskFactory?.GetHashCode() ?? 0);
            hash = hash * 23 + (TaskCompletionSource?.GetHashCode() ?? 0);
            return hash;
        }
#else
        return HashCode.Combine(TaskFactory, TaskCompletionSource);
#endif
    }

    public static bool operator ==(ActionTaskWrapper<TOutput> left, ActionTaskWrapper<TOutput> right) =>
        left.Equals(right);

    public static bool operator !=(ActionTaskWrapper<TOutput> left, ActionTaskWrapper<TOutput> right) =>
        !left.Equals(right);

#if NET6_0_OR_GREATER
    public void Deconstruct(out Func<Task<TOutput>> taskFactory, out TaskCompletionSource<TOutput> taskCompletionSource)
    {
        taskFactory = TaskFactory;
        taskCompletionSource = TaskCompletionSource;
    }
#endif
}

#if NET6_0_OR_GREATER
/// <summary>
/// A high-performance struct wrapper for channel-based item tasks to reduce heap allocations.
/// </summary>
public readonly struct ChannelItemTaskWrapper<TInput> : IEquatable<ChannelItemTaskWrapper<TInput>>
{
    public readonly TInput Input;
    public readonly Func<TInput, Task> TaskFactory;

    public ChannelItemTaskWrapper(TInput input, Func<TInput, Task> taskFactory)
    {
        Input = input;
        TaskFactory = taskFactory;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task Process(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        
        await TaskFactory.Invoke(Input).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ChannelItemTaskWrapper<TInput> other) =>
        EqualityComparer<TInput>.Default.Equals(Input, other.Input) &&
        ReferenceEquals(TaskFactory, other.TaskFactory);

    public override bool Equals(object? obj) =>
        obj is ChannelItemTaskWrapper<TInput> other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Input, TaskFactory);

    public static bool operator ==(ChannelItemTaskWrapper<TInput> left, ChannelItemTaskWrapper<TInput> right) =>
        left.Equals(right);

    public static bool operator !=(ChannelItemTaskWrapper<TInput> left, ChannelItemTaskWrapper<TInput> right) =>
        !left.Equals(right);

    public void Deconstruct(out TInput input, out Func<TInput, Task> taskFactory)
    {
        input = Input;
        taskFactory = TaskFactory;
    }
}

/// <summary>
/// A high-performance struct wrapper for channel-based item tasks with results to reduce heap allocations.
/// </summary>
public readonly struct ChannelItemTaskWrapper<TInput, TOutput> : IEquatable<ChannelItemTaskWrapper<TInput, TOutput>>
{
    public readonly TInput Input;
    public readonly Func<TInput, Task<TOutput>> TaskFactory;

    public ChannelItemTaskWrapper(TInput input, Func<TInput, Task<TOutput>> taskFactory)
    {
        Input = input;
        TaskFactory = taskFactory;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<TOutput> Process(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        
        return await TaskFactory.Invoke(Input).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ChannelItemTaskWrapper<TInput, TOutput> other) =>
        EqualityComparer<TInput>.Default.Equals(Input, other.Input) &&
        ReferenceEquals(TaskFactory, other.TaskFactory);

    public override bool Equals(object? obj) =>
        obj is ChannelItemTaskWrapper<TInput, TOutput> other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Input, TaskFactory);

    public static bool operator ==(ChannelItemTaskWrapper<TInput, TOutput> left, ChannelItemTaskWrapper<TInput, TOutput> right) =>
        left.Equals(right);

    public static bool operator !=(ChannelItemTaskWrapper<TInput, TOutput> left, ChannelItemTaskWrapper<TInput, TOutput> right) =>
        !left.Equals(right);

    public void Deconstruct(out TInput input, out Func<TInput, Task<TOutput>> taskFactory)
    {
        input = Input;
        taskFactory = TaskFactory;
    }
}
#endif