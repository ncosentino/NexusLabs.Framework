using System;
using System.Threading;
using System.Threading.Tasks;

using NexusLabs.Framework;

using TUnit.Assertions.Exceptions;

namespace NexusLabs.TUnit.Assertions.Tests;

public sealed class TriedAssertionTests
{
    [Test]
    public async Task Succeeded_TriedEx_ReturnsOriginalValue(
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var expected = new object();
        TriedEx<object> result = expected;

        var actual = await Assert.That(result)
            .Succeeded()
            .Because("The result should expose its successful value");

        await Assert.That(ReferenceEquals(expected, actual))
            .IsTrue()
            .Because("Succeeded should preserve the original value instance");
    }

    [Test]
    public async Task Succeeded_TriedNullEx_ReturnsNull(
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        TriedNullEx<string?> result = (string?)null;

        var actual = await Assert.That(result)
            .Succeeded()
            .Because("A null value is valid for TriedNullEx");

        await Assert.That(actual).IsNull();
    }

    [Test]
    public async Task Succeeded_FailedResult_IncludesReasonAndOriginalError(
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var original = new InvalidOperationException("original failure");
        TriedEx<int> result = original;

        Func<Task> action = async () =>
        {
            _ = await Assert.That(result)
                .Succeeded()
                .Because("The operation should have succeeded");
        };

        var exception = (await Assert.That(action).Throws<AssertionException>())!;

        await Assert.That(exception.Message)
            .Contains("The operation should have succeeded");
        await Assert.That(exception.Message)
            .Contains(nameof(InvalidOperationException));
        await Assert.That(exception.Message)
            .Contains(original.Message);
        await Assert.That(ReferenceEquals(original, exception.InnerException))
            .IsTrue()
            .Because("The assertion should preserve the captured exception");
    }

    [Test]
    public async Task Failed_ReturnsOriginalException(
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var original = new InvalidOperationException("expected failure");
        TriedEx<int> result = original;

        var actual = await Assert.That(result)
            .Failed()
            .Because("The result should have failed");

        await Assert.That(ReferenceEquals(original, actual))
            .IsTrue()
            .Because("Failed should preserve the original exception instance");
    }

    [Test]
    public async Task FailedWith_AssignableException_ReturnsOriginalException(
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var original = new ArgumentNullException("value");
        TriedNullEx<string?> result = original;

        var actual = await Assert.That(result)
            .Failed()
            .With<ArgumentException>()
            .Because("The result should contain a validation failure");

        await Assert.That(ReferenceEquals(original, actual))
            .IsTrue()
            .Because("Typed failure assertions should preserve exception identity");
    }

    [Test]
    public async Task Failed_SuccessfulResult_IncludesReason(
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        TriedEx<int> result = 42;

        Func<Task> action = async () =>
        {
            _ = await Assert.That(result)
                .Failed()
                .Because("The operation should have failed");
        };

        var exception = (await Assert.That(action).Throws<AssertionException>())!;

        await Assert.That(exception.Message)
            .Contains("The operation should have failed");
        await Assert.That(exception.Message)
            .Contains("the result succeeded");
    }

    [Test]
    public async Task FailedWith_WrongExceptionType_ReportsBothTypes(
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var original = new InvalidOperationException("wrong failure");
        TriedEx<int> result = original;

        Func<Task> action = async () =>
        {
            _ = await Assert.That(result)
                .Failed()
                .With<ArgumentException>()
                .Because("The result should contain an argument failure");
        };

        var exception = (await Assert.That(action).Throws<AssertionException>())!;

        await Assert.That(exception.Message)
            .Contains(nameof(InvalidOperationException));
        await Assert.That(exception.Message)
            .Contains(nameof(ArgumentException));
        await Assert.That(ReferenceEquals(original, exception.InnerException))
            .IsTrue()
            .Because("The assertion should preserve the actual captured exception");
    }
}
