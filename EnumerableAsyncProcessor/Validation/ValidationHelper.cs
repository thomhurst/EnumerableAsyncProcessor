using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace EnumerableAsyncProcessor.Validation;

/// <summary>
/// Provides validation methods for the EnumerableAsyncProcessor library.
/// </summary>
internal static class ValidationHelper
{
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
    /// Validates that a count of executions is valid.
    /// </summary>
    /// <param name="count">The count value to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the count is negative.</exception>
    public static void ValidateCount(int count, [CallerArgumentExpression(nameof(count))] string? paramName = null)
    {
        ThrowIfNegative(count, paramName);
    }

    /// <summary>
    /// Validates that a batch size is positive.
    /// </summary>
    /// <param name="batchSize">The batch size to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the batch size is negative or zero.</exception>
    public static void ValidateBatchSize(int batchSize, [CallerArgumentExpression(nameof(batchSize))] string? paramName = null)
    {
        ThrowIfNegativeOrZero(batchSize, paramName);
    }

    /// <summary>
    /// Validates that a parallelism level is positive.
    /// </summary>
    /// <param name="levelsOfParallelism">The parallelism level to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the parallelism level is negative or zero.</exception>
    public static void ValidateParallelism(int levelsOfParallelism, [CallerArgumentExpression(nameof(levelsOfParallelism))] string? paramName = null)
    {
        ThrowIfNegativeOrZero(levelsOfParallelism, paramName);
    }

    /// <summary>
    /// Validates that a TimeSpan is valid for timed operations.
    /// </summary>
    /// <param name="timeSpan">The TimeSpan to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the TimeSpan is negative.</exception>
    public static void ValidateTimeSpan(TimeSpan timeSpan, [CallerArgumentExpression(nameof(timeSpan))] string? paramName = null)
    {
        ThrowIfNegative(timeSpan, paramName);
    }

    /// <summary>
    /// Validates that a CancellationTokenSource is not null and not already cancelled.
    /// </summary>
    /// <param name="cancellationTokenSource">The CancellationTokenSource to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentNullException">Thrown when the CancellationTokenSource is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the CancellationTokenSource has already been cancelled.</exception>
    public static void ValidateCancellationTokenSource([NotNull] CancellationTokenSource? cancellationTokenSource, [CallerArgumentExpression(nameof(cancellationTokenSource))] string? paramName = null)
    {
        ThrowIfNull(cancellationTokenSource, paramName);

        if (cancellationTokenSource.IsCancellationRequested)
        {
            throw new ArgumentException($"'{paramName}' has already been cancelled.", paramName);
        }
    }
}
