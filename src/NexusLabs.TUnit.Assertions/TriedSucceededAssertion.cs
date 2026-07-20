using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using NexusLabs.Framework;

using TUnit.Assertions.Core;

namespace NexusLabs.TUnit.Assertions;

/// <summary>
/// TUnit assertion that requires a <see cref="TriedEx{T}"/> to be successful
/// and returns its non-null value when awaited.
/// </summary>
/// <typeparam name="T">The successful value type.</typeparam>
public sealed class TriedSucceededAssertion<T> : Assertion<T>
{
    internal TriedSucceededAssertion(AssertionContext<TriedEx<T>> context)
        : base(context.Map(TriedAssertionMapping.GetSuccessfulValue))
    {
    }

    /// <summary>
    /// Adds context explaining why the result is expected to be successful.
    /// </summary>
    /// <param name="message">The reason displayed when the assertion fails.</param>
    /// <returns>This assertion for fluent chaining.</returns>
    public new TriedSucceededAssertion<T> Because(string message)
    {
        base.Because(message);
        return this;
    }

    /// <summary>
    /// Returns an awaiter that produces the successful value.
    /// </summary>
    /// <returns>An awaiter for the assertion result.</returns>
    public new TaskAwaiter<T> GetAwaiter() => GetValueAsync().GetAwaiter();

    protected override Task<AssertionResult> CheckAsync(
        EvaluationMetadata<T> metadata) =>
        TriedAssertionResult.FromException(metadata.Exception);

    protected override string GetExpectation() => "the result to have succeeded";

#pragma warning disable NLF0020 // The awaiter protocol has no CancellationToken parameter.
    private async Task<T> GetValueAsync() => (await AssertAsync())!;
#pragma warning restore NLF0020
}
