using System.Data.Common;

using Dapper;

using NexusLabs.Framework.Data;

using Xunit;

namespace NexusLabs.Data.Sql.MySql.Tests;

public sealed class DapperCompatibilityTests
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task FactoryConnection_PreservesStandardAdoNetRuntimeTypes()
    {
        var factory = CreateFactory();
        await using var connection = await factory.CreateNewConnectionAsync(_ct);

        var dbConnection = Assert.IsAssignableFrom<DbConnection>(connection);
        await using var dbCommand = Assert.IsAssignableFrom<DbCommand>(
            connection.CreateCommand());

        Assert.Same(dbConnection, dbCommand.Connection);
    }

    [Fact]
    public async Task ExecuteAsync_WithPreCanceledCommand_PassesDapperTypeGates()
    {
        var factory = CreateFactory();
        await using IAsyncDbConnection connection =
            await factory.CreateNewConnectionAsync(_ct);
        using var cancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(_ct);
        cancellationTokenSource.Cancel();
        var command = new CommandDefinition(
            "SELECT 1",
            cancellationToken: cancellationTokenSource.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => connection.ExecuteAsync(command));
    }

    [Fact]
    public async Task QueryAsync_WithPreCanceledCommand_PassesDapperTypeGates()
    {
        var factory = CreateFactory();
        await using IAsyncDbConnection connection =
            await factory.CreateNewConnectionAsync(_ct);
        using var cancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(_ct);
        cancellationTokenSource.Cancel();
        var command = new CommandDefinition(
            "SELECT 1",
            cancellationToken: cancellationTokenSource.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => connection.QueryAsync<int>(command));
    }

    [Fact]
    public async Task ExecuteReaderAsync_WithPreCanceledCommand_PassesDapperTypeGates()
    {
        var factory = CreateFactory();
        await using IAsyncDbConnection connection =
            await factory.CreateNewConnectionAsync(_ct);
        using var cancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(_ct);
        cancellationTokenSource.Cancel();
        var command = new CommandDefinition(
            "SELECT 1",
            cancellationToken: cancellationTokenSource.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => connection.ExecuteReaderAsync(command));
    }

    private static MySqlConnectionFactory CreateFactory() =>
        new(new MySqlConnectionConfiguration(
            Server: "127.0.0.1",
            Port: 1,
            Database: "test",
            Username: "test",
            Password: "test",
            MinimumPoolSize: 0,
            SslMode: "Disabled",
            MaximumPoolSize: 1));
}
