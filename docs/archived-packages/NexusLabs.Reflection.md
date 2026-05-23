# NexusLabs.Reflection (archived in v1.0.0)

## Status

Archived. The 0.x line (latest: `0.0.7`) remains on nuget.org. Source is preserved on the `release/0.x` branch.

## Why archived

The package was a thin collection of reflection helpers, mostly used internally to wrap less-than-ideal interactions with third-party libraries. Maintaining it as a public package no longer made sense.

## Last shipped versions

- `NexusLabs.Reflection` 0.0.7

## Replacement guidance

There is no drop-in replacement. Consumers should inline the small set of helpers they actually used. The most common patterns:

- `Type.GetMethod(...)` / `MethodInfo.Invoke(...)` directly.
- `Activator.CreateInstance(...)` for constructor invocation.
- `Microsoft.Extensions.Internal.PropertyHelper` (when applicable) for property reflection.

## Source recovery

```
git fetch origin release/0.x
git switch release/0.x
# Source lives at NexusLabs.Reflection/
```
