namespace NexusLabs.Data.Sql.MySql;

/// <summary>
/// Default record-based implementation of <see cref="IMySqlConnectionConfiguration"/>.
/// </summary>
public sealed record MySqlConnectionConfiguration(
    string Server,
    int Port,
    string Database,
    string Username,
    string Password,
    int MinimumPoolSize = 1,
    string SslMode = "Preferred",
    int MaximumPoolSize = 50) :
    IMySqlConnectionConfiguration;
