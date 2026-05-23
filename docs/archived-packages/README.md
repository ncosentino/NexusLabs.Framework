# Archived Packages

This directory documents NexusLabs packages that were part of the `NexusLabs.Framework` repo through 0.1.x but were archived (not carried forward) as part of the 0.2.0 modernization.

For each archived package:

- The **0.x package versions remain available on nuget.org** — pinning to the latest 0.x release is the supported way to keep consuming the package.
- The **source remains in the `release/0.x` git branch** of this repository for archival reference.
- The **package ID was marked as Deprecated on nuget.org** with a reason and (where applicable) an alternative.

## Archived in 0.2.0

| Package | Replacement / Guidance | Notes |
|---|---|---|
| [`NexusLabs.Autofac`](NexusLabs.Autofac.md) | None — DI strategy changed | Repo standardized on Needlr for DI. |
| [`NexusLabs.Collections.Generic`](NexusLabs.Collections.Generic.md) | Mostly BCL replacements | Trie, LRU, BulkObservableCollection were retired; most other types had BCL equivalents (`Enumerable.Chunk`, `OfType`, `Random.Shared.GetItems`, etc.). |
| [`NexusLabs.Contracts`](NexusLabs.Contracts.md) | BCL: `ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrEmpty` | The BCL ships these since .NET 7. |
| [`NexusLabs.Dynamo`](NexusLabs.Dynamo.md) | Source generation | Runtime dynamic interface generation displaced by source generators. |
| [`NexusLabs.Reflection`](NexusLabs.Reflection.md) | None — case-by-case migration | Reflection helpers no longer maintained; consumers should inline the small set of helpers they used. |
| [`NexusLabs.Testing.Xunit`](NexusLabs.Testing.Xunit.md) | Successor library planned (separate effort) | New assertion library based on C# 14 `extension(Assert)` blocks and xunit.v3 is planned but not yet released. |
