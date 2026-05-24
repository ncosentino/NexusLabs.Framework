namespace NexusLabs.Data.Sql.MySql;

/// <summary>
/// Configuration values consumed by <see cref="MySqlConnectionFactory"/> to build a pooled
/// MySQL connection string.
/// </summary>
public interface IMySqlConnectionConfiguration
{
    /// <summary>Optional database (schema) name. When null or whitespace, no Database= entry is set.</summary>
    string? Database { get; }

    /// <summary>Database password. Required.</summary>
    string Password { get; }

    /// <summary>Database server host. Required.</summary>
    string Server { get; }

    /// <summary>Database server port. Must be a positive integer.</summary>
    int Port { get; }

    /// <summary>Database user. Required.</summary>
    string Username { get; }

    /// <summary>Minimum number of pooled connections kept warm. Default 1.</summary>
    int MinimumPoolSize { get; }

    /// <summary>Maximum number of pooled connections this factory may open.</summary>
    int MaximumPoolSize { get; }

    /// <summary>
    /// SSL mode passed straight through to the MySql.Data SslMode connection-string option
    /// (e.g. "Required", "Preferred", "None"). The value is validated by the underlying driver
    /// at connect time.
    /// </summary>
    string SslMode { get; }
}
