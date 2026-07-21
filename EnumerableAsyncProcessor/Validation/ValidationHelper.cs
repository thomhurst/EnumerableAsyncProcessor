using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace EnumerableAsyncProcessor.Validation;

/// <summary>
/// Provides validation methods for the EnumerableAsyncProcessor library.
/// </summary>
internal static class ValidationHelper
{
    /// <summary>
    /// Validates that an object is not null.
    /// </summary>
    /// <typeparam name="T">The type of the object to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
    public static void ThrowIfNull<T>([NotNull] T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
    }

    /// <summary>
    /// Validates that a value is not negative using modern or fallback methods.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative.</exception>
    public static void ThrowIfNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value, paramName);
    }

    /// <summary>
    /// Validates that a value is not negative or zero using modern or fallback methods.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative or zero.</exception>
    public static void ThrowIfNegativeOrZero(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, paramName);
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
