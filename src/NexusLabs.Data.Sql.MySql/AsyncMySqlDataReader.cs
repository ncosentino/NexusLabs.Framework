using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;

using NexusLabs.Framework;
using NexusLabs.Framework.Data;

namespace NexusLabs.Data.Sql.MySql;

/// <summary>
/// Internal adapter that wraps a <see cref="MySqlDataReader"/> and exposes it as an
/// <see cref="IAsyncDbDataReader"/>. Every async method delegates to the underlying reader's
/// own async implementation - never falls through to a sync-over-async base.
/// </summary>
internal sealed class AsyncMySqlDataReader : IAsyncDbDataReader
{
    [TransfersOwnership]
    private readonly MySqlDataReader _reader;

    public AsyncMySqlDataReader(MySqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        _reader = reader;
    }

    public object this[int i] => _reader[i];
    public object this[string name] => _reader[name];

    public int Depth => _reader.Depth;
    public int FieldCount => _reader.FieldCount;
    public bool IsClosed => _reader.IsClosed;
    public int RecordsAffected => _reader.RecordsAffected;

    public void Close() => _reader.Close();
    public Task CloseAsync() => _reader.CloseAsync();

    public void Dispose() => _reader.Dispose();
    public ValueTask DisposeAsync() => _reader.DisposeAsync();

    public bool GetBoolean(int i) => _reader.GetBoolean(i);
    public byte GetByte(int i) => _reader.GetByte(i);
    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) =>
        _reader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
    public char GetChar(int i) => _reader.GetChar(i);
    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) =>
        _reader.GetChars(i, fieldoffset, buffer, bufferoffset, length);
    public IDataReader GetData(int i) => ((IDataRecord)_reader).GetData(i);
    public string GetDataTypeName(int i) => _reader.GetDataTypeName(i);
    public DateTime GetDateTime(int i) => _reader.GetDateTime(i);
    public decimal GetDecimal(int i) => _reader.GetDecimal(i);
    public double GetDouble(int i) => _reader.GetDouble(i);

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    public Type GetFieldType(int i) => _reader.GetFieldType(i);

    public float GetFloat(int i) => _reader.GetFloat(i);
    public Guid GetGuid(int i) => _reader.GetGuid(i);
    public short GetInt16(int i) => _reader.GetInt16(i);
    public int GetInt32(int i) => _reader.GetInt32(i);
    public long GetInt64(int i) => _reader.GetInt64(i);
    public string GetName(int i) => _reader.GetName(i);
    public int GetOrdinal(string name) => _reader.GetOrdinal(name);
    public DataTable? GetSchemaTable() => _reader.GetSchemaTable();
    public string GetString(int i) => _reader.GetString(i);
    public object GetValue(int i) => _reader.GetValue(i);
    public int GetValues(object[] values) => _reader.GetValues(values);

    public bool IsDBNull(int i) => _reader.IsDBNull(i);
    public Task<bool> IsDBNullAsync(int ordinal) => _reader.IsDBNullAsync(ordinal);
    public Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) =>
        _reader.IsDBNullAsync(ordinal, cancellationToken);

    public bool NextResult() => _reader.NextResult();

    public bool Read() => _reader.Read();
    public Task<bool> ReadAsync(CancellationToken cancellationToken) =>
        _reader.ReadAsync(cancellationToken);
}
