using System.Data;

using MySql.Data.MySqlClient;

using NexusLabs.Framework.Data;

using Xunit;

namespace NexusLabs.Data.Sql.MySql.Tests;

public sealed class AsyncMySqlConnectionTests
{
    [Fact]
    public void Adapter_ExposesConnectionString()
    {
        using var inner = new MySqlConnection("Server=localhost;Database=db;Uid=u;Pwd=p");
        using IAsyncDbConnection sut = new AsyncMySqlConnection(inner);

        Assert.Contains("localhost", sut.ConnectionString!, System.StringComparison.OrdinalIgnoreCase);
        Assert.Equal("db", sut.Database);
    }

    [Fact]
    public void Adapter_ExposesClosedStateBeforeOpen()
    {
        using var inner = new MySqlConnection("Server=localhost;Database=db;Uid=u;Pwd=p");
        using IAsyncDbConnection sut = new AsyncMySqlConnection(inner);

        Assert.Equal(ConnectionState.Closed, sut.State);
    }

    [Fact]
    public void Adapter_CreateAsyncCommand_ReturnsAsyncMySqlCommand()
    {
        using var inner = new MySqlConnection("Server=localhost;Database=db;Uid=u;Pwd=p");
        using IAsyncDbConnection sut = new AsyncMySqlConnection(inner);

        using var cmd = sut.CreateAsyncCommand();

        Assert.IsType<AsyncMySqlCommand>(cmd);
    }

    [Fact]
    public void Adapter_CommandText_RoundTrips()
    {
        using var inner = new MySqlConnection("Server=localhost;Database=db;Uid=u;Pwd=p");
        using IAsyncDbConnection sut = new AsyncMySqlConnection(inner);
        using var cmd = sut.CreateAsyncCommand();

        cmd.CommandText = "SELECT 1";

        Assert.Equal("SELECT 1", cmd.CommandText);
    }
}
