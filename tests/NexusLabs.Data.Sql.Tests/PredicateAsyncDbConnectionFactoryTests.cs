using System;
using System.Threading;
using System.Threading.Tasks;

using Moq;

using NexusLabs.Framework.Data;

using Xunit;

namespace NexusLabs.Data.Sql.Tests;

public sealed class PredicateAsyncDbConnectionFactoryTests : IDisposable
{
    private readonly MockRepository _mocks = new(MockBehavior.Strict);

    public void Dispose() => _mocks.VerifyAll();

    [Fact]
    public void Constructor_RejectsNullArgs()
    {
        var inner = _mocks.Create<IAsyncDbConnection>().Object;

        Assert.Throws<ArgumentNullException>(
            () => new PredicateAsyncDbConnectionFactory(
                null!,
                _ => Task.FromResult(inner)));

        Assert.Throws<ArgumentNullException>(
            () => new PredicateAsyncDbConnectionFactory("cs", null!));
    }

    [Fact]
    public void ConnectionString_ReturnsExactValuePassedAtConstruction()
    {
        var inner = _mocks.Create<IAsyncDbConnection>().Object;
        var sut = new PredicateAsyncDbConnectionFactory(
            "server=x;database=y",
            _ => Task.FromResult(inner));

        Assert.Equal("server=x;database=y", sut.ConnectionString);
    }

    [Fact]
    public async Task CreateNewConnectionAsync_DelegatesAndDoesNotOpen()
    {
        var inner = _mocks.Create<IAsyncDbConnection>();

        var sut = new PredicateAsyncDbConnectionFactory(
            "cs",
            _ => Task.FromResult(inner.Object));

        var result = await sut.CreateNewConnectionAsync();

        Assert.Same(inner.Object, result);
        inner.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Never);
        inner.Verify(c => c.OpenAsync(), Times.Never);
    }

    [Fact]
    public async Task OpenNewConnectionAsync_DefaultOpensConnection()
    {
        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = new PredicateAsyncDbConnectionFactory(
            "cs",
            _ => Task.FromResult(inner.Object));

        var result = await sut.OpenNewConnectionAsync();

        Assert.Same(inner.Object, result);
        inner.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OpenNewConnectionAsync_OnOpenFailure_DisposesConnection()
    {
        var inner = _mocks.Create<IAsyncDbConnection>();
        inner.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("nope"));
        inner.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var sut = new PredicateAsyncDbConnectionFactory(
            "cs",
            _ => Task.FromResult(inner.Object));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.OpenNewConnectionAsync());

        inner.Verify(c => c.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task OpenNewConnectionAsync_RespectsCustomOpenCallback()
    {
        var inner = _mocks.Create<IAsyncDbConnection>();
        var customCalled = false;

        var sut = new PredicateAsyncDbConnectionFactory(
            "cs",
            _ => Task.FromResult(inner.Object),
            _ => { customCalled = true; return Task.FromResult(inner.Object); });

        var result = await sut.OpenNewConnectionAsync();

        Assert.True(customCalled);
        Assert.Same(inner.Object, result);
        inner.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
