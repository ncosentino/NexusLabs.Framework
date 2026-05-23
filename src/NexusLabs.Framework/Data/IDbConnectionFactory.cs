using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLabs.Framework.Data;

public interface IDbConnectionFactory
{
    string ConnectionString { get; }

    Task<IAsyncDbConnection> CreateNewConnectionAsync(
        CancellationToken cancellationToken = default);

    Task<IAsyncDbConnection> OpenNewConnectionAsync(
        CancellationToken cancellationToken = default);
}