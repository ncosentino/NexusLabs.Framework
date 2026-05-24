using System;

namespace NexusLabs.Data.Sql;

/// <summary>A snapshot of a single open connection's origin.</summary>
/// <param name="Callstack">Call stack captured at the point of <c>OpenAsync</c>.</param>
/// <param name="OpenedAt">UTC timestamp of the open call.</param>
public sealed record OpenConnectionEntry(string Callstack, DateTimeOffset OpenedAt);
