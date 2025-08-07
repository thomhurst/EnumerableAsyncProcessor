using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace EnumerableAsyncProcessor.Validation;

/// <summary>
/// Provides validation methods and constants for the EnumerableAsyncProcessor library.
/// </summary>
internal static class ValidationHelper
{
    /// <summary>
    /// Maximum number of concurrent tasks allowed to prevent memory issues and system overload.
    /// </summary>
    public const int MAX_CONCURRENT_TASKS = 10000;

    /// <summary>
    /// Maximum batch size allowed for batch processors.
    /// </summary>
    public const int MAX_BATCH_SIZE = 10000;

    /// <summary>
    /// Minimum batch size allowed for batch processors.
    /// </summary>
    public const int MIN_BATCH_SIZE = 1;

    /// <summary>
    /// Maximum levels of parallelism allowed for parallel processors.
    /// </summary>
    public const int MAX_PARALLELISM = 10000;

    /// <summary>
    /// Minimum levels of parallelism allowed for parallel processors.
    /// </summary>
    public const int MIN_PARALLELISM = 1;

    /// <summary>
    /// Maximum TimeSpan value allowed for timed processors (24 hours).
    /// </summary>
    public static readonly TimeSpan MAX_TIMESPAN = TimeSpan.FromDays(1);

    /// <summary>
    /// Minimum TimeSpan value allowed for timed processors.
    /// </summary>
    public static readonly TimeSpan MIN_TIMESPAN = TimeSpan.Zero;

    /// <summary>
    /// Validates that an object is not null using modern or fallback methods.
    /// </summary>
    /// <typeparam name="T">The type of the object to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
    public static void ThrowIfNull<T>([NotNull] T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(value, paramName);
#else
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
#endif
    }

    /// <summary>
    /// Validates that a value is not negative using modern or fallback methods.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative.</exception>
    public static void ThrowIfNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfNegative(value, paramName);
#else
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be a non-negative value.");
        }
#endif
    }

    /// <summary>
    /// Validates that a value is not negative or zero using modern or fallback methods.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative or zero.</exception>
    public static void ThrowIfNegativeOrZero(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, paramName);
#else
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be a positive value.");
        }
#endif
    }

    /// <summary>
    /// Validates that a TimeSpan value is not negative.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative.</exception>
    public static void ThrowIfNegative(TimeSpan value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be a non-negative TimeSpan.");
        }
    }

    /// <summary>
    /// Validates that a TimeSpan value is positive.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative or zero.</exception>
    public static void ThrowIfNegativeOrZero(TimeSpan value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be a positive TimeSpan.");
        }
    }

    /// <summary>
    /// Validates that a count value is within acceptable limits for concurrent processing.
    /// </summary>
    /// <param name="count">The count value to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the count is out of acceptable range.</exception>
    public static void ValidateCount(int count, [CallerArgumentExpression(nameof(count))] string? paramName = null)
    {
        ThrowIfNegative(count, paramName);

        if (count > MAX_CONCURRENT_TASKS)
        {
            throw new ArgumentOutOfRangeException(paramName, count, 
                $"'{paramName}' cannot exceed {MAX_CONCURRENT_TASKS} to prevent system overload. Consider processing in smaller batches.");
        }
    }

    /// <summary>
    /// Validates that a batch size is within acceptable limits.
    /// </summary>
    /// <param name="batchSize">The batch size to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the batch size is out of acceptable range.</exception>
    public static void ValidateBatchSize(int batchSize, [CallerArgumentExpression(nameof(batchSize))] string? paramName = null)
    {
        if (batchSize < MIN_BATCH_SIZE)
        {
            throw new ArgumentOutOfRangeException(paramName, batchSize, 
                $"'{paramName}' must be at least {MIN_BATCH_SIZE}.");
        }

        if (batchSize > MAX_BATCH_SIZE)
        {
            throw new ArgumentOutOfRangeException(paramName, batchSize, 
                $"'{paramName}' cannot exceed {MAX_BATCH_SIZE} to prevent memory issues.");
        }
    }

    /// <summary>
    /// Validates that a parallelism level is within acceptable limits.
    /// </summary>
    /// <param name="levelsOfParallelism">The parallelism level to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the parallelism level is out of acceptable range.</exception>
    public static void ValidateParallelism(int levelsOfParallelism, [CallerArgumentExpression(nameof(levelsOfParallelism))] string? paramName = null)
    {
        if (levelsOfParallelism < MIN_PARALLELISM)
        {
            throw new ArgumentOutOfRangeException(paramName, levelsOfParallelism, 
                $"'{paramName}' must be at least {MIN_PARALLELISM}.");
        }
    }

    /// <summary>
    /// Validates that a TimeSpan is within acceptable limits for timed operations.
    /// </summary>
    /// <param name="timeSpan">The TimeSpan to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the TimeSpan is out of acceptable range.</exception>
    public static void ValidateTimeSpan(TimeSpan timeSpan, [CallerArgumentExpression(nameof(timeSpan))] string? paramName = null)
    {
        ThrowIfNegative(timeSpan, paramName);

        if (timeSpan > MAX_TIMESPAN)
        {
            throw new ArgumentOutOfRangeException(paramName, timeSpan, 
                $"'{paramName}' cannot exceed {MAX_TIMESPAN.TotalHours} hours.");
        }
    }

    /// <summary>
    /// Validates that an enumerable collection is not null and provides optimization hints.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="items">The collection to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <returns>True if the collection is empty (optimization hint).</returns>
    /// <exception cref="ArgumentNullException">Thrown when the collection is null.</exception>
    public static bool ValidateEnumerable<T>([NotNull] IEnumerable<T>? items, [CallerArgumentExpression(nameof(items))] string? paramName = null)
    {
        ThrowIfNull(items, paramName);

        // Check if collection is empty for potential optimization
        if (items is ICollection<T> collection)
        {
            return collection.Count == 0;
        }

        // For other enumerables, we need to check if it has any elements
        // This is a more expensive operation but necessary for optimization
        return !items.Any();
    }

    /// <summary>
    /// Validates that a CancellationTokenSource is not null.
    /// </summary>
    /// <param name="cancellationTokenSource">The CancellationTokenSource to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentNullException">Thrown when the CancellationTokenSource is null.</exception>
    public static void ValidateCancellationTokenSource([NotNull] CancellationTokenSource? cancellationTokenSource, [CallerArgumentExpression(nameof(cancellationTokenSource))] string? paramName = null)
    {
        ThrowIfNull(cancellationTokenSource, paramName);

        if (cancellationTokenSource.IsCancellationRequested)
        {
            throw new ArgumentException($"'{paramName}' has already been cancelled.", paramName);
        }
    }

    /// <summary>
    /// Provides a warning for very large collections that might impact performance.
    /// </summary>
    /// <param name="count">The count of items to process.</param>
    /// <returns>A warning message if the count is very large, otherwise null.</returns>
    public static string? GetPerformanceWarning(int count)
    {
        return count switch
        {
            > 100000 => $"Processing {count:N0} items may consume significant memory and time. Consider processing in smaller batches.",
            > 50000 => $"Processing {count:N0} items may impact performance. Monitor memory usage.",
            _ => null
        };
    }
}