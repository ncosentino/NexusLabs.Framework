using System;
using System.Collections.Generic;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

using NexusLabs.CodeAnalysis.Testing.TUnit;

using TUnit.Assertions.Exceptions;

using Xunit;

using Assert = Xunit.Assert;

namespace NexusLabs.CodeAnalysis.Testing.TUnit.Tests;

/// <summary>
/// xunit.v3-based tests for the TUnit-flavored <see cref="TUnitVerifier"/>. The verifier
/// itself is framework-agnostic - it just throws <see cref="AssertionException"/> on
/// failure - so it can be exercised under any test runner. These tests cover every method
/// on the <see cref="IVerifier"/> contract.
/// </summary>
public sealed class TUnitVerifierTests
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public void Empty_EmptyCollection_DoesNotThrow()
    {
        var sut = new TUnitVerifier();
        sut.Empty("items", Array.Empty<int>());
    }

    [Fact]
    public void Empty_NonEmptyCollection_Throws()
    {
        var sut = new TUnitVerifier();
        var ex = Assert.Throws<AssertionException>(() => sut.Empty("items", new[] { 1, 2, 3 }));
        Assert.Contains("'items' is not empty", ex.Message);
    }

    [Fact]
    public void NotEmpty_NonEmptyCollection_DoesNotThrow()
    {
        var sut = new TUnitVerifier();
        sut.NotEmpty("items", new[] { 1 });
    }

    [Fact]
    public void NotEmpty_EmptyCollection_Throws()
    {
        var sut = new TUnitVerifier();
        var ex = Assert.Throws<AssertionException>(() => sut.NotEmpty("items", Array.Empty<int>()));
        Assert.Contains("'items' is empty", ex.Message);
    }

    [Fact]
    public void LanguageIsSupported_CSharp_DoesNotThrow()
    {
        var sut = new TUnitVerifier();
        sut.LanguageIsSupported(LanguageNames.CSharp);
    }

    [Fact]
    public void LanguageIsSupported_VisualBasic_DoesNotThrow()
    {
        var sut = new TUnitVerifier();
        sut.LanguageIsSupported(LanguageNames.VisualBasic);
    }

    [Fact]
    public void LanguageIsSupported_UnknownLanguage_Throws()
    {
        var sut = new TUnitVerifier();
        var ex = Assert.Throws<AssertionException>(() => sut.LanguageIsSupported("F#"));
        Assert.Contains("Unsupported Language: 'F#'", ex.Message);
    }

    [Fact]
    public void Equal_EqualValues_DoesNotThrow()
    {
        var sut = new TUnitVerifier();
        sut.Equal(1, 1);
        sut.Equal("a", "a");
        sut.Equal<int?>(null, null);
    }

    [Fact]
    public void Equal_UnequalValues_Throws()
    {
        var sut = new TUnitVerifier();
        var ex = Assert.Throws<AssertionException>(() => sut.Equal(1, 2));
        Assert.Contains("items not equal", ex.Message);
        Assert.Contains("expected:'1'", ex.Message);
        Assert.Contains("actual:'2'", ex.Message);
    }

    [Fact]
    public void Equal_CustomMessageUsedOnFailure()
    {
        var sut = new TUnitVerifier();
        var ex = Assert.Throws<AssertionException>(() => sut.Equal(1, 2, "custom failure message"));
        Assert.Contains("custom failure message", ex.Message);
    }

    [Fact]
    public void True_TrueValue_DoesNotThrow()
    {
        var sut = new TUnitVerifier();
        sut.True(true);
    }

    [Fact]
    public void True_FalseValue_Throws()
    {
        var sut = new TUnitVerifier();
        var ex = Assert.Throws<AssertionException>(() => sut.True(false));
        Assert.Contains("Expected value to be 'true' but was 'false'", ex.Message);
    }

    [Fact]
    public void False_FalseValue_DoesNotThrow()
    {
        var sut = new TUnitVerifier();
        sut.False(false);
    }

    [Fact]
    public void False_TrueValue_Throws()
    {
        var sut = new TUnitVerifier();
        var ex = Assert.Throws<AssertionException>(() => sut.False(true));
        Assert.Contains("Expected value to be 'false' but was 'true'", ex.Message);
    }

    [Fact]
    public void Fail_AlwaysThrows()
    {
        var sut = new TUnitVerifier();
        var ex = Assert.Throws<AssertionException>(() => sut.Fail("explicit failure"));
        Assert.Contains("explicit failure", ex.Message);
    }

    [Fact]
    public void Fail_NoMessage_UsesDefault()
    {
        var sut = new TUnitVerifier();
        var ex = Assert.Throws<AssertionException>(() => sut.Fail());
        Assert.Contains("Verification failed for an unspecified reason", ex.Message);
    }

    [Fact]
    public void SequenceEqual_EqualSequences_DoesNotThrow()
    {
        var sut = new TUnitVerifier();
        sut.SequenceEqual(new[] { 1, 2, 3 }, new List<int> { 1, 2, 3 });
    }

    [Fact]
    public void SequenceEqual_SameReference_DoesNotThrow()
    {
        var sut = new TUnitVerifier();
        var both = new[] { 1, 2, 3 };
        sut.SequenceEqual<int>(both, both);
    }

    [Fact]
    public void SequenceEqual_DifferentLengths_Throws()
    {
        var sut = new TUnitVerifier();
        Assert.Throws<AssertionException>(() => sut.SequenceEqual(new[] { 1, 2 }, new[] { 1, 2, 3 }));
    }

    [Fact]
    public void SequenceEqual_DifferentElements_Throws()
    {
        var sut = new TUnitVerifier();
        Assert.Throws<AssertionException>(() => sut.SequenceEqual(new[] { 1, 2, 3 }, new[] { 1, 2, 4 }));
    }

    [Fact]
    public void SequenceEqual_CustomComparer_Used()
    {
        var sut = new TUnitVerifier();
        var comparer = StringComparer.OrdinalIgnoreCase;
        sut.SequenceEqual(new[] { "A", "B" }, new[] { "a", "b" }, comparer);
    }

    [Fact]
    public void PushContext_ReturnsTUnitVerifierWithContextPrefix()
    {
        var sut = new TUnitVerifier();
        var pushed = sut.PushContext("OuterContext");

        Assert.IsType<TUnitVerifier>(pushed);

        var ex = Assert.Throws<AssertionException>(() => pushed.True(false));
        Assert.Contains("Context: OuterContext", ex.Message);
        Assert.Contains("Expected value to be 'true'", ex.Message);
    }

    [Fact]
    public void PushContext_Nested_BothContextsAppear()
    {
        var sut = new TUnitVerifier();
        var nested = sut.PushContext("Outer").PushContext("Inner");

        var ex = Assert.Throws<AssertionException>(() => nested.Fail("boom"));
        Assert.Contains("Context: Outer", ex.Message);
        Assert.Contains("Context: Inner", ex.Message);
        Assert.Contains("boom", ex.Message);
    }
}
