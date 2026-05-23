using System;
using System.Diagnostics.CodeAnalysis;

using NexusLabs.Framework;

using Xunit;
using Xunit.Sdk;

namespace NexusLabs.Xunit.Assertions.Tests;

[ExcludeFromCodeCoverage]
public sealed class AssertAugmentationsTests
{
    [Fact]
    public void True_LazyMessage_NotInvokedOnSuccess()
    {
        var invoked = false;
        Assert.True(true, () => { invoked = true; return "should never appear"; });

        Assert.False(invoked, "Lazy message factory should not run on a passing assertion.");
    }

    [Fact]
    public void True_LazyMessage_InvokedOnFailure()
    {
        var invoked = false;
        var ex = Assert.Throws<TrueException>(() =>
            Assert.True(false, () => { invoked = true; return "context message here"; }));

        Assert.True(invoked, "Lazy message factory should run on a failing assertion.");
        Assert.Contains("context message here", ex.Message);
    }

    [Fact]
    public void False_LazyMessage_InvokedOnFailure()
    {
        var ex = Assert.Throws<FalseException>(() =>
            Assert.False(true, () => "false-context"));

        Assert.Contains("false-context", ex.Message);
    }

    [Fact]
    public void EqualIgnoreLineEndingStyle_TreatsCrlfAndLfAsEqual()
    {
        var crlf = "line1\r\nline2\r\nline3";
        var lf = "line1\nline2\nline3";

        Assert.EqualIgnoreLineEndingStyle(crlf, lf);
    }

    [Fact]
    public void EqualIgnoreLineEndingStyle_StillFailsOnRealDifference()
    {
        var a = "alpha";
        var b = "bravo";

        Assert.ThrowsAny<XunitException>(() =>
            Assert.EqualIgnoreLineEndingStyle(a, b));
    }

    [Fact]
    public void TrySucceeded_SuccessfulTriedEx_ReturnsValue()
    {
        TriedEx<int> tried = 42;

        var value = Assert.TrySucceeded(tried, "should not fail");

        Assert.Equal(42, value);
    }

    [Fact]
    public void TrySucceeded_FailedTriedEx_ThrowsWithCallerMessage()
    {
        TriedEx<int> tried = new InvalidOperationException("inner-failure-text");

        var thrown = Assert.ThrowsAny<TrueException>(() =>
            Assert.TrySucceeded(tried, "caller-context-here"));

        Assert.Contains("caller-context-here", thrown.Message);
        Assert.Contains("inner-failure-text", thrown.Message);
    }

    [Fact]
    public void TryFailed_FailedTriedExWithMatchingExceptionType_ReturnsTheException()
    {
        var original = new InvalidOperationException("the original");
        TriedEx<int> tried = original;

        var captured = Assert.TryFailed<int, InvalidOperationException>(tried, "context");

        Assert.Same(original, captured);
    }

    [Fact]
    public void TryFailed_SuccessfulTriedEx_ThrowsXunitException()
    {
        TriedEx<int> tried = 7;

        Assert.Throws<XunitException>(() =>
            Assert.TryFailed<int, InvalidOperationException>(tried, "expected failure"));
    }

    [Fact]
    public void NotNull_NullValue_ThrowsWithCallerMessage()
    {
        string? value = null;

        var thrown = Assert.ThrowsAny<XunitException>(() =>
            Assert.NotNull(value, "caller-said-this"));

        Assert.Contains("caller-said-this", thrown.Message);
    }

    [Fact]
    public void NotNull_NonNullValue_DoesNotThrow()
    {
        Assert.NotNull("hello", "should never fire");
    }

    [Fact]
    public void GreaterThanZero_NegativeValue_Throws()
    {
        Assert.ThrowsAny<XunitException>(() =>
            Assert.GreaterThanZero(-1, "should-be-positive"));
    }

    [Fact]
    public void GreaterThanZero_PositiveValue_DoesNotThrow()
    {
        Assert.GreaterThanZero(5, "should-be-positive");
    }
}
