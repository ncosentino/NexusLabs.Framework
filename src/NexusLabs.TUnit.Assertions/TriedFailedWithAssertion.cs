using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using TUnit.Assertions.Core;

namespace NexusLabs.TUnit.Assertions;

/// <summary>
/// TUnit assertion that requires a Tried result to contain an exception
/// assignable to <typeparamref name="TException"/>.
/// </summary>
/// <typeparam name="TException">The expected exception type.</typeparam>
public sealed class TriedFailedWithAssertion<TException> : Assertion<TException>
    where TException : Exception
{
    internal TriedFailedWithAssertion(AssertionContext<TException> context)
        : base(context)
    {
    }

    /// <summary>
    /// Adds context explaining why the result is expected to contain this
    /// exception type.
    /// </summary>
    /// <param name="message">The reason displayed when the assertion fails.</param>
    /// <returns>This assertion for fluent chaining.</returns>
    public new TriedFailedWithAssertion<TException> Because(string message)
    {
        base.Because(message);
        return this;
    }

    /// <summary>
    /// Returns an awaiter that produces the captured typed exception.
    /// </summary>
    /// <returns>An awaiter for the assertion result.</returns>
    public new TaskAwaiter<TException> GetAwaiter() => GetErrorAsync().GetAwaiter();

    protected override Task<AssertionResult> CheckAsync(
        EvaluationMetadata<TException> metadata) =>
        TriedAssertionResult.FromException(metadata.Exception);

    protected override string GetExpectation() =>
        $"the result to have failed with {typeof(TException).Name}";

#pragma warning disable NLF0020 // The awaiter protocol has no CancellationToken parameter.
    private async Task<TException> GetErrorAsync() => (await AssertAsync())!;
#pragma warning restore NLF0020
}
