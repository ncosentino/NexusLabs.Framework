using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

using Xunit;

namespace NexusLabs.StronglyTypedIds.Tests;

public sealed class UuidV7IdentifierServiceCollectionExtensionsTests
{
    [Fact]
    public void AddUuidV7IdentifierGeneration_WithExistingTimeProvider_PreservesRegistration()
    {
        var timestamp = new DateTimeOffset(
            year: 2026,
            month: 7,
            day: 23,
            hour: 19,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(timestamp);
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);

        services.AddUuidV7IdentifierGeneration();

        using var provider = services.BuildServiceProvider();
        var resolvedTimeProvider = provider.GetRequiredService<TimeProvider>();
        var generator =
            provider.GetRequiredService<IUuidV7IdentifierGenerator<TestUuidV7Id>>();
        var identifier = generator.Create();

        Assert.Same(timeProvider, resolvedTimeProvider);
        Assert.Equal(7, identifier.Value.Version);
        Assert.Equal(
            timestamp.ToUnixTimeMilliseconds(),
            UuidV7GuidTestHelper.ReadUnixTimeMilliseconds(identifier.Value));
    }

    [Fact]
    public void AddUuidV7IdentifierGeneration_WithoutTimeProvider_RegistersSystemTime()
    {
        var services = new ServiceCollection();

        services.AddUuidV7IdentifierGeneration();

        using var provider = services.BuildServiceProvider();
        var timeProvider = provider.GetRequiredService<TimeProvider>();
        var generator =
            provider.GetRequiredService<IUuidV7IdentifierGenerator<TestUuidV7Id>>();

        Assert.Same(TimeProvider.System, timeProvider);
        Assert.Equal(7, generator.Create().Value.Version);
    }

}
