using Xunit;

namespace NexusLabs.Data.Sql.MySql.Tests;

public sealed class MySqlConnectionConfigurationTests
{
    [Fact]
    public void DefaultValues_MatchSpec()
    {
        var sut = new MySqlConnectionConfiguration(
            Server: "localhost",
            Port: 3306,
            Database: "test",
            Username: "user",
            Password: "pw");

        Assert.Equal(1, sut.MinimumPoolSize);
        Assert.Equal(50, sut.MaximumPoolSize);
        Assert.Equal("Preferred", sut.SslMode);
    }

    [Fact]
    public void RecordEquality_Works()
    {
        var a = new MySqlConnectionConfiguration(
            Server: "h", Port: 3306, Database: "d", Username: "u", Password: "p");
        var b = new MySqlConnectionConfiguration(
            Server: "h", Port: 3306, Database: "d", Username: "u", Password: "p");

        Assert.Equal(a, b);
        Assert.NotSame(a, b);
    }
}
