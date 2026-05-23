# NexusLabs.Dynamo (archived in v1.0.0)

## Status

Archived. The 0.x line (latest: `0.1.3`) remains on nuget.org. Source is preserved on the `release/0.x` branch.

## Why archived

The runtime dynamic-interface-generation use case has largely been displaced by source generators. The dependencies (`Castle.Core 4.4.1`, `ImpromptuInterface 7.0.1`) are old and untracked.

## Last shipped versions

- `NexusLabs.Dynamo` 0.1.3

## Replacement guidance

For most cases where dynamic interface generation was useful, a source generator is the modern answer. Specific options:

- **Roslyn source generators** to emit concrete implementations of interfaces at compile time.
- **`Microsoft.Extensions.AI`** / **`Microsoft.Extensions.DependencyInjection`** auto-implementation patterns.
- **Castle.DynamicProxy** 5.x (still maintained) for runtime proxies if you really need them.

## Source recovery

```
git fetch origin release/0.x
git switch release/0.x
# Source lives at NexusLabs.Dynamo/
```
