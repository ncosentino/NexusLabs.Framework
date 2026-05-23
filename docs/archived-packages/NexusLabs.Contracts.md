# NexusLabs.Contracts (archived in v1.0.0)

## Status

Archived. The 0.x line (latest: `0.0.7`) remains on nuget.org. Source is preserved on the `release/0.x` branch.

## Why archived

The BCL has shipped equivalent helpers since .NET 7. The package no longer earns its place.

## Last shipped versions

- `NexusLabs.Contracts` 0.0.7

## Replacement guidance

| 0.x helper | BCL replacement |
|---|---|
| `Contract.RequiresNotNull(value, nameof(value))` | `ArgumentNullException.ThrowIfNull(value)` (net6+) |
| `Contract.RequiresNotNullOrEmpty(value, nameof(value))` | `ArgumentException.ThrowIfNullOrEmpty(value)` (net7+) |
| `Contract.RequiresNotNullOrWhiteSpace(value, nameof(value))` | `ArgumentException.ThrowIfNullOrWhiteSpace(value)` (net8+) |

## Source recovery

```
git fetch origin release/0.x
git switch release/0.x
# Source lives at NexusLabs.Contracts/
```
