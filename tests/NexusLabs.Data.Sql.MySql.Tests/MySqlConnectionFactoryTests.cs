using System;
using System.Threading;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;

using NexusLabs.Framework.Data;

using Xunit;

namespace NexusLabs.Data.Sql.MySql.Tests;

public sealed class MySqlConnectionFactoryTests
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static MySqlConnectionConfiguration MakeConfig(
        string? server = "localhost",
        int port = 3306,
        string? database = "test_db",
        string? username = "user",
        string? password = "pw",
        int minimumPoolSize = 1,
        int maximumPoolSize = 50,
        string sslMode = "Preferred")
    {
        return new MySqlConnectionConfiguration(
            Server: server!,
            Port: port,
            Database: database!,
            Username: username!,
            Password: password!,
            MinimumPoolSize: minimumPoolSize,
            MaximumPoolSize: maximumPoolSize,
            SslMode: sslMode);
    }

    [Fact]
    public void Ctor_NullConfig_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MySqlConnectionFactory(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_BlankServer_Throws(string? server)
    {
        var cfg = MakeConfig(server: server);
        Assert.ThrowsAny<ArgumentException>(() => new MySqlConnectionFactory(cfg));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_BlankUsername_Throws(string? username)
    {
        var cfg = MakeConfig(username: username);
        Assert.ThrowsAny<ArgumentException>(() => new MySqlConnectionFactory(cfg));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Ctor_EmptyPassword_Throws(string? password)
    {
        var cfg = MakeConfig(password: password);
        Assert.ThrowsAny<ArgumentException>(() => new MySqlConnectionFactory(cfg));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Ctor_NonPositivePort_Throws(int port)
    {
        var cfg = MakeConfig(port: port);
        Assert.Throws<ArgumentOutOfRangeException>(() => new MySqlConnectionFactory(cfg));
    }

    [Fact]
    public void Ctor_InvalidSslMode_Throws()
    {
        var cfg = MakeConfig(sslMode: "NotARealMode");
        Assert.ThrowsAny<ArgumentException>(() => new MySqlConnectionFactory(cfg));
    }

    [Fact]
    public void ConnectionString_RoundTripsAllConfiguredValues()
    {
        var sut = new MySqlConnectionFactory(MakeConfig(
            server: "db.example.com",
            port: 13306,
            database: "my_app",
            username: "app_user",
            password: "s3cret",
            minimumPoolSize: 2,
            maximumPoolSize: 25,
            sslMode: "Required"));

        var parsed = new MySqlConnectionStringBuilder(sut.ConnectionString);

        Assert.Equal("db.example.com", parsed.Server);
        Assert.Equal((uint)13306, parsed.Port);
        Assert.Equal("my_app", parsed.Database);
        Assert.Equal("app_user", parsed.UserID);
        Assert.Equal("s3cret", parsed.Password);
        Assert.Equal((uint)2, parsed.MinimumPoolSize);
        Assert.Equal((uint)25, parsed.MaximumPoolSize);
        Assert.Equal(MySqlSslMode.Required, parsed.SslMode);
        Assert.True(parsed.Pooling);
        Assert.True(parsed.AllowUserVariables);
        Assert.Equal(
            MySqlConnectionFactory.DefaultConnectionLifetimeSeconds,
            parsed.ConnectionLifeTime);
    }

    [Fact]
    public void ConnectionString_PasswordContainingSemicolon_IsSafelyEscaped()
    {
        var sut = new MySqlConnectionFactory(MakeConfig(
            password: "abc;DROP TABLE users;--"));

        var parsed = new MySqlConnectionStringBuilder(sut.ConnectionString);

        Assert.Equal("abc;DROP TABLE users;--", parsed.Password);
    }

    [Fact]
    public void ConnectionString_PasswordContainingSingleQuote_IsSafelyEscaped()
    {
        var sut = new MySqlConnectionFactory(MakeConfig(
            password: "ab'cd\"ef={ghi}"));

        var parsed = new MySqlConnectionStringBuilder(sut.ConnectionString);

        Assert.Equal("ab'cd\"ef={ghi}", parsed.Password);
    }

    [Fact]
    public void ConnectionString_BlankDatabase_OmitsDatabaseEntry()
    {
        var sut = new MySqlConnectionFactory(MakeConfig(database: ""));

        var parsed = new MySqlConnectionStringBuilder(sut.ConnectionString);

        Assert.True(string.IsNullOrEmpty(parsed.Database));
    }

    [Fact]
    public async Task CreateNewConnectionAsync_ReturnsClosedConnection()
    {
        var sut = new MySqlConnectionFactory(MakeConfig());

        await using var connection = await sut.CreateNewConnectionAsync(_ct);

        Assert.Equal(System.Data.ConnectionState.Closed, connection.State);
    }

    [Fact]
    public async Task CreateNewConnectionAsync_HonorsCancellation()
    {
        var sut = new MySqlConnectionFactory(MakeConfig());
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.CreateNewConnectionAsync(cts.Token));
    }

    [Fact]
    public async Task OpenNewConnectionAsync_OnUnreachableServer_WrapsInInvalidOperationException()
    {
        var sut = new MySqlConnectionFactory(MakeConfig(
            server: "127.0.0.1",
            port: 1,
            database: "anything"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.OpenNewConnectionAsync(_ct));

        Assert.Contains("127.0.0.1:1", ex.Message);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public async Task OpenNewConnectionAsync_HonorsPreCancellation()
    {
        var sut = new MySqlConnectionFactory(MakeConfig());
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.OpenNewConnectionAsync(cts.Token));
    }
}
