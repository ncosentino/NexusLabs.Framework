# NexusLabs.Autofac (archived in v1.0.0)

## Status

Archived. The 0.x line (latest: `0.0.4`) remains on nuget.org. Source is preserved on the `release/0.x` branch of this repository.

## Why archived

NexusLabs standardized on the Needlr DI framework. The Autofac wrapper is no longer maintained.

## Last shipped versions

- `NexusLabs.Autofac` 0.0.4 — supports Autofac 6.4.x

## Replacement guidance

There is no drop-in replacement. Consumers who require Autofac extensibility (e.g. wire tapping) should either:

1. Pin to `NexusLabs.Autofac 0.0.4` indefinitely (works against Autofac 6.x).
2. Implement equivalent extensions against current Autofac (8.x) directly in their own codebase.

## Source recovery

```
git fetch origin release/0.x
git switch release/0.x
# NexusLabs.Autofac source lives at NexusLabs.Autofac/
```
