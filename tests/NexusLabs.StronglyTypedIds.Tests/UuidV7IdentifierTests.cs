using System.Globalization;

using Microsoft.Extensions.Time.Testing;

using Xunit;

namespace NexusLabs.StronglyTypedIds.Tests;

public sealed class UuidV7IdentifierTests
{
    [Fact]
    public void Create_WithTimeProvider_UsesProvidedTimestamp()
    {
        var timestamp = new DateTimeOffset(
            year: 2026,
            month: 7,
            day: 23,
            hour: 18,
            minute: 15,
            second: 30,
            offset: TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(timestamp);

        var identifier = TestUuidV7Id.Create(timeProvider);

        Assert.Equal(7, identifier.Value.Version);
        Assert.Equal(
            timestamp.ToUnixTimeMilliseconds(),
            UuidV7GuidTestHelper.ReadUnixTimeMilliseconds(identifier.Value));
    }

    [Fact]
    public void Create_WithoutTimeProvider_CreatesUuidV7()
    {
        var identifier = TestUuidV7Id.Create();

        Assert.Equal(7, identifier.Value.Version);
    }

    [Fact]
    public void Constructor_WithExternalGuid_PreservesArbitraryValue()
    {
        var externalValue = Guid.Parse(
            "5e7d9a31-8bb4-4f82-8baa-814813645a57",
            CultureInfo.InvariantCulture);

        var identifier = new TestUuidV7Id(externalValue);

        Assert.Equal(externalValue, identifier.Value);
        Assert.Equal(4, identifier.Value.Version);
    }

}
