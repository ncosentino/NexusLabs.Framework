using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Moq;

using NexusLabs.Framework.IO;

using Xunit;

namespace NexusLabs.Framework.Tests.IO;

public sealed class TemporaryFileFactoryTests : IDisposable
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;
    private readonly string _testRoot =
        Path.Combine(Path.GetTempPath(), "nlf-tff-tests-" + Guid.NewGuid().ToString("N"));

    public TemporaryFileFactoryTests() => Directory.CreateDirectory(_testRoot);

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    [Fact]
    public void Create_CreatesFileThatExists()
    {
        var factory = new TemporaryFileFactory();

        using var file = factory.Create(new TemporaryFileOptions { RootPath = _testRoot });

        Assert.True(File.Exists(file.Path), "temp file should exist after Create");
    }

    [Fact]
    public void Create_WithCreateEmptyFileFalse_DoesNotCreateFile()
    {
        var factory = new TemporaryFileFactory();

        using var file = factory.Create(new TemporaryFileOptions
        {
            RootPath = _testRoot,
            CreateEmptyFile = false,
        });

        Assert.False(File.Exists(file.Path), "file should not exist when CreateEmptyFile is false");
    }

    [Fact]
    public void Create_HonorsExtension()
    {
        var factory = new TemporaryFileFactory();

        using var file = factory.Create(new TemporaryFileOptions
        {
            RootPath = _testRoot,
            Extension = ".tmp",
        });

        Assert.EndsWith(".tmp", file.Path);
    }

    [Fact]
    public void Dispose_DeletesFile()
    {
        var factory = new TemporaryFileFactory();
        string path;
        using (var file = factory.Create(new TemporaryFileOptions { RootPath = _testRoot }))
        {
            path = file.Path;
            Assert.True(File.Exists(path), "temp file should exist before dispose");
        }

        Assert.False(File.Exists(path), "temp file should be deleted after dispose");
    }

    [Fact]
    public async Task DisposeAsync_DeletesFile()
    {
        var factory = new TemporaryFileFactory();
        string path;
        await using (var file = factory.Create(new TemporaryFileOptions { RootPath = _testRoot }))
        {
            path = file.Path;
            await File.WriteAllTextAsync(file.Path, "data", _ct);
        }

        Assert.False(File.Exists(path), "temp file should be deleted after async dispose");
    }

    [Fact]
    public void Dispose_DeletesReadOnlyFile()
    {
        var factory = new TemporaryFileFactory();
        string path;
        using (var file = factory.Create(new TemporaryFileOptions { RootPath = _testRoot }))
        {
            path = file.Path;
            File.SetAttributes(path, FileAttributes.ReadOnly);
        }

        Assert.False(File.Exists(path), "read-only temp file should be deleted on dispose");
    }

    [Fact]
    public void Create_TwoCalls_ProduceDistinctPaths()
    {
        var factory = new TemporaryFileFactory();

        using var first = factory.Create(new TemporaryFileOptions { RootPath = _testRoot });
        using var second = factory.Create(new TemporaryFileOptions { RootPath = _testRoot });

        Assert.NotEqual(first.Path, second.Path);
    }

    [Fact]
    public void Dispose_WhenFileLocked_InvokesOnCleanupErrorOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip("Deleting an open file only fails on Windows.");
        }

        var factory = new TemporaryFileFactory();
        Exception? captured = null;
        var file = factory.Create(new TemporaryFileOptions
        {
            RootPath = _testRoot,
            OnCleanupError = ex => captured = ex,
        });

        var stream = new FileStream(file.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        file.Dispose();
        stream.Dispose();

        Assert.NotNull(captured);
        Assert.IsType<IOException>(captured);
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
        var factory = new TemporaryFileFactory(logger.Object, _ => (_ => throw boom));

        using (factory.Create(new TemporaryFileOptions { RootPath = _testRoot, CreateEmptyFile = false }))
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
        var factory = new TemporaryFileFactory(logger.Object, _ => (_ => throw boom));

        using (factory.Create(new TemporaryFileOptions
        {
            RootPath = _testRoot,
            CreateEmptyFile = false,
            OnCleanupError = ex => captured = ex,
        }))
        {
        }

        Assert.Same(boom, captured);
        repo.VerifyAll();
    }
}
