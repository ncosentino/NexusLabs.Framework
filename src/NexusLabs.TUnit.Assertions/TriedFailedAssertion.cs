using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using NexusLabs.Framework;

using TUnit.Assertions.Core;

namespace NexusLabs.TUnit.Assertions;

/// <summary>
/// TUnit assertion that requires a Tried result to have failed and returns its
/// original exception when awaited.
/// </summary>
/// <typeparam name="T">The result value type.</typeparam>
public sealed class TriedFailedAssertion<T> : Assertion<Exception>
{
    internal TriedFailedAssertion(AssertionContext<TriedEx<T>> context)
        : base(context.Map(TriedAssertionMapping.GetFailure))
    {
    }

    internal TriedFailedAssertion(AssertionContext<TriedNullEx<T?>> context)
        : base(context.Map(TriedAssertionMapping.GetFailure))
    {
    }

    /// <summary>
    /// Adds context explaining why the result is expected to have failed.
    /// </summary>
    /// <param name="message">The reason displayed when the assertion fails.</param>
    /// <returns>This assertion for fluent chaining.</returns>
    public new TriedFailedAssertion<T> Because(string message)
    {
        base.Because(message);
        return this;
    }

    /// <summary>
    /// Requires the captured exception to be assignable to
    /// <typeparamref name="TException"/> and returns it strongly typed when awaited.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <returns>An awaitable assertion that produces the typed exception.</returns>
    public TriedFailedWithAssertion<TException> With<TException>()
        where TException : Exception
    {
        AppendExpression($".With<{typeof(TException).Name}>()");
        return new TriedFailedWithAssertion<TException>(
            Context.Map(TriedAssertionMapping.GetFailure<TException>));
    }

    /// <summary>
    /// Returns an awaiter that produces the captured exception.
    /// </summary>
    /// <returns>An awaiter for the assertion result.</returns>
    public new TaskAwaiter<Exception> GetAwaiter() => GetErrorAsync().GetAwaiter();

    protected override Task<AssertionResult> CheckAsync(
        EvaluationMetadata<Exception> metadata) =>
        TriedAssertionResult.FromException(metadata.Exception);

    protected override string GetExpectation() => "the result to have failed";

#pragma warning disable NLF0020 // The awaiter protocol has no CancellationToken parameter.
    private async Task<Exception> GetErrorAsync() => (await AssertAsync())!;
#pragma warning restore NLF0020
}
