# NexusLabs.Testing.Xunit (archived in 0.2.0)

## Status

Archived. The 0.x line (latest: `0.0.1`) remains on nuget.org. Source is preserved on the `release/0.x` branch.

## Why archived

The 0.x version was a thin wrapper that never iterated past `0.0.1`. A successor library is planned but has not yet shipped.

## Last shipped versions

- `NexusLabs.Testing.Xunit` 0.0.1

## Successor (planned, separate effort)

A new assertion library is being planned that:

- Uses C# 14 `extension(Assert)` blocks to augment `xUnit.Assert`.
- Integrates tightly with `NexusLabs.Framework` result types (`TriedEx<T>`, `TriedNullEx<T?>`, `ExceptionHelper`).
- Provides HTTP-response assertions for integration tests.
- Targets xunit.v3 (3.x) and net10+.

When that library ships, this page will be updated with the package ID and migration notes.

## Source recovery

```
git fetch origin release/0.x
git switch release/0.x
# Source lives at NexusLabs.Testing.Xunit/
```
