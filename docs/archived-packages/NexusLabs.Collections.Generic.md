# NexusLabs.Collections.Generic (archived in v1.0.0)

## Status

Archived. The 0.x line (latest: `0.0.23`) remains on nuget.org. Source is preserved on the `release/0.x` branch.

## Why archived

The package was a mix of useful primitives and types that have been displaced by the modern BCL. Maintaining the package as a single unit no longer made sense.

## Last shipped versions

- `NexusLabs.Collections.Generic` 0.0.23

## Replacement guidance

| Type | Replacement |
|---|---|
| `LruCache<TKey, TValue>` / `ICache<TKey, TValue>` | [`BitFaster.Caching`](https://www.nuget.org/packages/BitFaster.Caching/) — already recommended via `[Obsolete]` in 0.x. |
| `IEnumerableExtensions.Batch` | `Enumerable.Chunk<T>` (BCL net6+). |
| `IEnumerableExtensions.Random` / `RandomOrDefault` | `Random.Shared.GetItems<T>(...)` (BCL net8+) or simple reservoir sampling. |
| `IEnumerableExtensions.TakeTypes<T2>` | `.OfType<T2>()` (BCL). |
| `IEnumerableExtensions.Repeat` | `Enumerable.Repeat(obj, count)` (BCL). |
| `Trie` / `ITrie` | No drop-in BCL equivalent. Inline the small implementation if needed. |
| `BulkObservableCollection<T>` | No drop-in BCL equivalent. Inline if needed (WPF/MVVM use case). |
| `CachedEnumerable<T>` | No drop-in BCL equivalent. Inline if needed. |

## Source recovery

```
git fetch origin release/0.x
git switch release/0.x
# Source lives at NexusLabs.Collections.Generic/
```
