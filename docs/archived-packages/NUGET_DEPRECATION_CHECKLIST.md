# nuget.org Deprecation Checklist (external manual step)

The v1.0.0 modernization archives six NexusLabs packages from this repository.
Their source has been removed from the v1 branch but the package IDs remain on
nuget.org with their last 0.x versions. To complete the archival cleanly, each
package ID **must** be marked as Deprecated on nuget.org via the package owner
dashboard.

This is a manual step that cannot be automated from the repo.

## Steps per package

For each package ID below, log in to nuget.org as a package owner, navigate to
the package management page, and:

1. Open the **"Deprecate"** tab for the latest published version.
2. Choose "Apply the deprecation to all versions of this package."
3. Select reasons: at least **"Legacy"** (and **"Other"** if a different package
   takes over the use case).
4. Set the **"Alternate Package ID"** when one applies (see table below).
5. Paste the **custom message** below into the message box.
6. Save.

After deprecation, the package remains installable but installs will surface a
clear warning to consumers.

## Package list

| Package ID | Alternate Package ID | Custom message |
|---|---|---|
| `NexusLabs.Autofac` | (none) | Archived as of NexusLabs.Framework v1.0.0. The 0.x line remains available; source is preserved on the `release/0.x` branch. NexusLabs standardized on Needlr DI; this Autofac wrapper is no longer maintained. See https://github.com/ncosentino/NexusLabs.Framework/blob/master/docs/archived-packages/NexusLabs.Autofac.md |
| `NexusLabs.Collections.Generic` | (none — case-by-case BCL replacements) | Archived as of NexusLabs.Framework v1.0.0. Most types now have BCL equivalents (Enumerable.Chunk, OfType, Random.Shared.GetItems). See https://github.com/ncosentino/NexusLabs.Framework/blob/master/docs/archived-packages/NexusLabs.Collections.Generic.md |
| `NexusLabs.Contracts` | (none — use BCL `ArgumentNullException.ThrowIfNull` etc.) | Archived as of NexusLabs.Framework v1.0.0. The BCL ships equivalent helpers since .NET 7. See https://github.com/ncosentino/NexusLabs.Framework/blob/master/docs/archived-packages/NexusLabs.Contracts.md |
| `NexusLabs.Dynamo` | (none) | Archived as of NexusLabs.Framework v1.0.0. Runtime dynamic-interface generation displaced by source generators. See https://github.com/ncosentino/NexusLabs.Framework/blob/master/docs/archived-packages/NexusLabs.Dynamo.md |
| `NexusLabs.Reflection` | (none) | Archived as of NexusLabs.Framework v1.0.0. Consumers should inline the small set of helpers they used. See https://github.com/ncosentino/NexusLabs.Framework/blob/master/docs/archived-packages/NexusLabs.Reflection.md |
| `NexusLabs.Testing.Xunit` | (TBD — successor library planned) | Archived as of NexusLabs.Framework v1.0.0. A successor assertion library is planned but not yet released. See https://github.com/ncosentino/NexusLabs.Framework/blob/master/docs/archived-packages/NexusLabs.Testing.Xunit.md |

## After completion

Once each package ID is marked Deprecated, update `docs/archived-packages/README.md`
to note that the deprecation flags have been applied (no other source change needed).
