using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using NexusLabs.Framework.IO;

using Xunit;

namespace NexusLabs.Framework.Tests.IO;

public sealed class TemporaryDirectoryFactoryTests : IDisposable
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;
    private readonly string _testRoot =
        Path.Combine(Path.GetTempPath(), "nlf-tdf-tests-" + Guid.NewGuid().ToString("N"));

    public TemporaryDirectoryFactoryTests() => Directory.CreateDirectory(_testRoot);

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    [Fact]
    public void Create_CreatesDirectoryThatExists()
    {
        var factory = new TemporaryDirectoryFactory();

        using var dir = factory.Create();

        Assert.True(Directory.Exists(dir.Path), "temp directory should exist after Create");
    }

    [Fact]
    public void Create_HonorsRootPathAndPrefix()
    {
        var factory = new TemporaryDirectoryFactory();
        var options = new TemporaryDirectoryOptions { RootPath = _testRoot, Prefix = "pfx-" };

        using var dir = factory.Create(options);

        Assert.Equal(_testRoot, Path.GetDirectoryName(dir.Path));
        Assert.StartsWith("pfx-", Path.GetFileName(dir.Path));
    }

    [Fact]
    public void Dispose_DeletesDirectoryAndContents()
    {
        var factory = new TemporaryDirectoryFactory();
        string path;
        using (var dir = factory.Create(new TemporaryDirectoryOptions { RootPath = _testRoot }))
        {
            path = dir.Path;
            File.WriteAllText(Path.Combine(dir.Path, "a.txt"), "hello");
            Assert.True(File.Exists(Path.Combine(path, "a.txt")), "seed file should exist");
        }

        Assert.False(Directory.Exists(path), "temp directory should be deleted after dispose");
    }

    [Fact]
    public async Task DisposeAsync_DeletesDirectory()
    {
        var factory = new TemporaryDirectoryFactory();
        string path;
        await using (var dir = factory.Create(new TemporaryDirectoryOptions { RootPath = _testRoot }))
        {
            path = dir.Path;
            await File.WriteAllTextAsync(Path.Combine(dir.Path, "a.txt"), "hello", _ct);
        }

        Assert.False(Directory.Exists(path), "temp directory should be deleted after async dispose");
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var factory = new TemporaryDirectoryFactory();
        var dir = factory.Create(new TemporaryDirectoryOptions { RootPath = _testRoot });
        var path = dir.Path;

        dir.Dispose();
        dir.Dispose();

        Assert.False(Directory.Exists(path), "temp directory should be deleted");
    }

    [Fact]
    public void Dispose_DeletesDirectoryContainingReadOnlyFile()
    {
        var factory = new TemporaryDirectoryFactory();
        string path;
        using (var dir = factory.Create(new TemporaryDirectoryOptions { RootPath = _testRoot }))
        {
            path = dir.Path;
            var file = Path.Combine(dir.Path, "ro.txt");
            File.WriteAllText(file, "x");
            File.SetAttributes(file, FileAttributes.ReadOnly);
        }

        Assert.False(Directory.Exists(path), "read-only file must not block temp directory cleanup");
    }

    [Fact]
    public void Create_TwoCalls_ProduceDistinctPaths()
    {
        var factory = new TemporaryDirectoryFactory();

        using var first = factory.Create(new TemporaryDirectoryOptions { RootPath = _testRoot });
        using var second = factory.Create(new TemporaryDirectoryOptions { RootPath = _testRoot });

        Assert.NotEqual(first.Path, second.Path);
    }

    [Fact]
    public void Dispose_WhenDeleteFails_InvokesOnCleanupErrorAndDoesNotThrow()
    {
        var boom = new IOException("locked");
        Exception? captured = null;
        Func<CancellationToken, ValueTask> deleteOnce = _ => throw boom;
        var dir = new TemporaryDirectory(
            @"X:\does-not-matter",
            deleteOnce,
            executor: null,
            onCleanupError: ex => captured = ex);

        dir.Dispose();

        Assert.Same(boom, captured);
    }

    [Fact]
    public void Dispose_NoOnCleanupError_LogsViaFactoryLogger()
    {
        var repo = new MockRepository(MockBehavior.Strict);
        var logger = repo.Create<ILogger>();
        logger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);
        logger.Setup(l => l.Log(
            LogLevel.Warning,
            It.Is<EventId>(e => e.Id == TemporaryResourceLog.CleanupFailedEventId),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<IOException>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

        var boom = new IOException("locked");
        var factory = new TemporaryDirectoryFactory(logger.Object, _ => (_ => throw boom));

        using (factory.Create(new TemporaryDirectoryOptions { RootPath = _testRoot }))
        {
        }

        repo.VerifyAll();
    }

    [Fact]
    public void Dispose_WithOnCleanupError_DoesNotUseFactoryLogger()
    {
        var repo = new MockRepository(MockBehavior.Strict);
        var logger = repo.Create<ILogger>();

        var boom = new IOException("locked");
        Exception? captured = null;
        var factory = new TemporaryDirectoryFactory(logger.Object, _ => (_ => throw boom));

        using (factory.Create(new TemporaryDirectoryOptions
        {
            RootPath = _testRoot,
            OnCleanupError = ex => captured = ex,
        }))
        {
        }

        Assert.Same(boom, captured);
        repo.VerifyAll();
    }

    [Fact]
    public void Factory_AndHandle_AreMockable()
    {
        var repo = new MockRepository(MockBehavior.Strict);
        var handle = repo.Create<ITemporaryDirectory>();
        handle.Setup(h => h.Path).Returns(@"C:\temp\mocked");
        var factory = repo.Create<ITemporaryDirectoryFactory>();
        factory.Setup(f => f.Create()).Returns(handle.Object);

        var result = factory.Object.Create();

        Assert.Equal(@"C:\temp\mocked", result.Path);
        repo.VerifyAll();
    }
}
