using NexusLabs.Framework;

using TUnit.Assertions.Core;

namespace NexusLabs.TUnit.Assertions;

/// <summary>
/// TUnit assertion extensions for <see cref="TriedEx{T}"/> and
/// <see cref="TriedNullEx{T}"/> results.
/// </summary>
public static class TriedAssertionExtensions
{
    /// <summary>
    /// Asserts that <paramref name="source"/> contains a successful
    /// <see cref="TriedEx{T}"/> and returns its value when awaited.
    /// </summary>
    /// <typeparam name="T">The successful value type.</typeparam>
    /// <param name="source">The TUnit assertion source.</param>
    /// <returns>An awaitable assertion that produces the successful value.</returns>
    public static TriedSucceededAssertion<T> Succeeded<T>(
        this IAssertionSource<TriedEx<T>> source)
    {
        source.Context.ExpressionBuilder.Append(".Succeeded()");
        return new TriedSucceededAssertion<T>(source.Context);
    }

    /// <summary>
    /// Asserts that <paramref name="source"/> contains a successful
    /// <see cref="TriedNullEx{T}"/> and returns its possibly-null value when awaited.
    /// </summary>
    /// <typeparam name="T">The successful value type.</typeparam>
    /// <param name="source">The TUnit assertion source.</param>
    /// <returns>An awaitable assertion that produces the successful value.</returns>
    public static TriedNullSucceededAssertion<T> Succeeded<T>(
        this IAssertionSource<TriedNullEx<T?>> source)
    {
        source.Context.ExpressionBuilder.Append(".Succeeded()");
        return new TriedNullSucceededAssertion<T>(source.Context);
    }

    /// <summary>
    /// Asserts that <paramref name="source"/> contains a failed
    /// <see cref="TriedEx{T}"/> and returns its original exception when awaited.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="source">The TUnit assertion source.</param>
    /// <returns>An awaitable assertion that produces the captured exception.</returns>
    public static TriedFailedAssertion<T> Failed<T>(
        this IAssertionSource<TriedEx<T>> source)
    {
        source.Context.ExpressionBuilder.Append(".Failed()");
        return new TriedFailedAssertion<T>(source.Context);
    }

    /// <summary>
    /// Asserts that <paramref name="source"/> contains a failed
    /// <see cref="TriedNullEx{T}"/> and returns its original exception when awaited.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="source">The TUnit assertion source.</param>
    /// <returns>An awaitable assertion that produces the captured exception.</returns>
    public static TriedFailedAssertion<T> Failed<T>(
        this IAssertionSource<TriedNullEx<T?>> source)
    {
        source.Context.ExpressionBuilder.Append(".Failed()");
        return new TriedFailedAssertion<T>(source.Context);
    }
}
