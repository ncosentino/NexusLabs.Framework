using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NexusLabs.Data.Sql;

/// <summary>
/// Thread-safe registry of currently-open connections tracked by
/// <see cref="OpenTrackingDecorator"/>. Construct one per logical pool and share it across the
/// decorators wrapping that pool's connections.
/// </summary>
public sealed class OpenConnectionTracker
{
    private readonly ConcurrentDictionary<Guid, OpenConnectionEntry> _open = new();

    /// <summary>The current set of open connections, ordered by open time (oldest first).</summary>
    public IReadOnlyList<OpenConnectionEntry> GetOpenConnections() =>
        _open.Values.OrderBy(e => e.OpenedAt).ToArray();

    internal void Register(Guid id, OpenConnectionEntry entry) => _open[id] = entry;

    internal void Unregister(Guid id) => _open.TryRemove(id, out _);
}
