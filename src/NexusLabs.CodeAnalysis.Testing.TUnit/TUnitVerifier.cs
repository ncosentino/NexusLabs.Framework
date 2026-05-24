using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

using TUnit.Assertions.Exceptions;

namespace NexusLabs.CodeAnalysis.Testing.TUnit;

/// <summary>
/// <see cref="IVerifier"/> implementation that throws TUnit's
/// <see cref="AssertionException"/> on failure. Pair with
/// <c>CSharpAnalyzerTest&lt;TAnalyzer, TUnitVerifier&gt;</c> (or any of the other
/// <c>...&lt;TVerifier&gt;</c> harness types in <c>Microsoft.CodeAnalysis.Testing</c>) to use
/// the full Roslyn analyzer / code-fix / source-generator test harness from a TUnit-based
/// test project.
/// </summary>
/// <remarks>
/// <para>
/// Microsoft ships verifiers for xUnit (<c>Microsoft.CodeAnalysis.Testing.Verifiers.XUnit</c>),
/// NUnit, and MSTest, but not for TUnit. This class fills that gap.
/// </para>
/// <para>
/// Inherits <see cref="DefaultVerifier"/> and overrides every assertion method to wrap its
/// failure message in <see cref="AssertionException"/>. Reuses the base
/// <see cref="DefaultVerifier.CreateMessage"/> helper so context pushed via
/// <see cref="PushContext"/> is still prefixed onto the failure message.
/// </para>
/// </remarks>
public class TUnitVerifier : DefaultVerifier
{
    /// <summary>Initializes a new instance with an empty verification context stack.</summary>
    public TUnitVerifier()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance with the supplied verification context stack. Used by
    /// <see cref="PushContext"/> to chain nested verification contexts.
    /// </summary>
    /// <param name="context">The verification context stack; innermost label on top.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="context"/> is null.</exception>
    protected TUnitVerifier(ImmutableStack<string> context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override void Empty<T>(string collectionName, IEnumerable<T> collection)
    {
        if (collection?.Any() == true)
        {
            throw new AssertionException(CreateMessage($"'{collectionName}' is not empty"));
        }
    }

    /// <inheritdoc />
    public override void NotEmpty<T>(string collectionName, IEnumerable<T> collection)
    {
        if (collection?.Any() == false)
        {
            throw new AssertionException(CreateMessage($"'{collectionName}' is empty"));
        }
    }

    /// <inheritdoc />
    public override void LanguageIsSupported(string language)
    {
        if (language != LanguageNames.CSharp && language != LanguageNames.VisualBasic)
        {
            throw new AssertionException(CreateMessage($"Unsupported Language: '{language}'"));
        }
    }

    /// <inheritdoc />
    public override void Equal<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new AssertionException(
                CreateMessage(message ?? $"items not equal.  expected:'{expected}' actual:'{actual}'"));
        }
    }

    /// <inheritdoc />
    public override void True([DoesNotReturnIf(false)] bool assert, string? message = null)
    {
        if (!assert)
        {
            throw new AssertionException(CreateMessage(message ?? "Expected value to be 'true' but was 'false'"));
        }
    }

    /// <inheritdoc />
    public override void False([DoesNotReturnIf(true)] bool assert, string? message = null)
    {
        if (assert)
        {
            throw new AssertionException(CreateMessage(message ?? "Expected value to be 'false' but was 'true'"));
        }
    }

    /// <inheritdoc />
    [DoesNotReturn]
    public override void Fail(string? message = null)
    {
        throw new AssertionException(CreateMessage(message ?? "Verification failed for an unspecified reason."));
    }

    /// <inheritdoc />
    public override void SequenceEqual<T>(
        IEnumerable<T> expected,
        IEnumerable<T> actual,
        IEqualityComparer<T>? equalityComparer = null,
        string? message = null)
    {
        var comparer = equalityComparer ?? EqualityComparer<T>.Default;
        if (ReferenceEquals(expected, actual))
        {
            return;
        }
        if (expected is null || actual is null || !expected.SequenceEqual(actual, comparer))
        {
            throw new AssertionException(CreateMessage(message ?? "Sequences are not equal"));
        }
    }

    /// <inheritdoc />
    public override IVerifier PushContext(string context)
    {
        return new TUnitVerifier(Context.Push(context));
    }
}
