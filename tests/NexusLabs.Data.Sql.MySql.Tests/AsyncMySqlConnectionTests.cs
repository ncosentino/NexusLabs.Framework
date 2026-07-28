using System.Data;
using System.Data.Common;
using System.Reflection;

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
        Assert.IsAssignableFrom<DbCommand>(cmd);
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

    [Fact]
    public void Adapter_OverridesProviderNativeAsyncExecutionPaths()
    {
        AssertDeclaredOverride(
            nameof(DbCommand.ExecuteNonQueryAsync),
            BindingFlags.Instance | BindingFlags.Public,
            typeof(CancellationToken));
        AssertDeclaredOverride(
            nameof(DbCommand.ExecuteScalarAsync),
            BindingFlags.Instance | BindingFlags.Public,
            typeof(CancellationToken));
        AssertDeclaredOverride(
            "ExecuteDbDataReaderAsync",
            BindingFlags.Instance | BindingFlags.NonPublic,
            typeof(CommandBehavior),
            typeof(CancellationToken));
        AssertDeclaredOverride(
            nameof(DbCommand.PrepareAsync),
            BindingFlags.Instance | BindingFlags.Public,
            typeof(CancellationToken));
        AssertDeclaredOverride(
            nameof(DbCommand.DisposeAsync),
            BindingFlags.Instance | BindingFlags.Public);
    }

    [Fact]
    public void Adapter_PreservesExplicitIdbConnectionTransactionDispatch()
    {
        var interfaceMap = typeof(AsyncMySqlConnection)
            .GetInterfaceMap(typeof(IDbConnection));
        var beginTransactionTargets = interfaceMap.InterfaceMethods
            .Select((method, index) => (method, target: interfaceMap.TargetMethods[index]))
            .Where(x => x.method.Name == nameof(IDbConnection.BeginTransaction))
            .Select(x => x.target.DeclaringType)
            .ToArray();

        Assert.Equal(2, beginTransactionTargets.Length);
        Assert.All(
            beginTransactionTargets,
            declaringType => Assert.Equal(typeof(AsyncMySqlConnection), declaringType));
    }

    private static void AssertDeclaredOverride(
        string methodName,
        BindingFlags bindingFlags,
        params Type[] parameterTypes)
    {
        var method = typeof(AsyncMySqlCommand).GetMethod(
            methodName,
            bindingFlags,
            binder: null,
            parameterTypes,
            modifiers: null);

        Assert.NotNull(method);
        Assert.Equal(typeof(AsyncMySqlCommand), method.DeclaringType);
    }
}
