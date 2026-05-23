# Changelog

All notable changes to **NexusLabs.Framework** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] - Unreleased

Major modernization. Single surviving package (`NexusLabs.Framework`); six sibling packages archived (see "Archived packages" below). Every change in this release is intentional and was inventoried via `dotnet apicompat` against `NexusLabs.Framework 0.1.4` — see [docs/v1-breaking-changes.md](docs/v1-breaking-changes.md) for the full inventory and per-type replacement guidance.

### Removed (Breaking)

The following public types were removed from the package. The 0.x line remains on nuget.org if you need them; archived source is on the `release/0.x` branch.

- `NexusLabs.Framework.Cast` / `NexusLabs.Framework.ICast` — reflection-based magic-cast. Use `Convert.ChangeType`, generic math, or write the conversion you need at the callsite.
- `NexusLabs.Framework.OnlyOnce` / `NexusLabs.Framework.OnlyOnce<T>` / `NexusLabs.Framework.IOnlyOnce` — `Lazy<T>` does this trivially.
- `System.StringExtensions.ToStream(Encoding)` — inline at callsite: `new MemoryStream(encoding.GetBytes(str))`.
- `NexusLabs.Framework.IO.BlockingBufferStream` — use `System.IO.Pipelines` (`PipeReader`/`PipeWriter`).
- `System.Data.IAsyncDbDataReaderExtensions` (326 lines of `GetXAsync`/`GetXOrNullAsync` overloads) — use BCL `DbDataReader.GetFieldValueAsync<T>` + `IsDBNullAsync`.
- `System.Data.IDataReaderExtensions`, `System.Data.IDBCommandExtensions`, `System.Data.Common.DbDataReaderExtensions` — inline at consumer callsites.
- `NexusLabs.Framework.Data.PredicateMySqlConnectionFactory` — duplicate of a consumer-owned class.

### Changed (Breaking) — namespace pollution cleanup

The following types moved out of BCL namespaces. Consumers must add a new `using NexusLabs.Framework.*;` directive in every file that uses these types. Compile errors will pinpoint each callsite.

| Old fully-qualified name | New fully-qualified name |
|---|---|
| `System.Data.IAsyncDbCommand` | `NexusLabs.Framework.Data.IAsyncDbCommand` |
| `System.Data.IAsyncDbConnection` | `NexusLabs.Framework.Data.IAsyncDbConnection` |
| `System.Data.IAsyncDbDataReader` | `NexusLabs.Framework.Data.IAsyncDbDataReader` |
| `System.Data.IDbConnectionFactory` | `NexusLabs.Framework.Data.IDbConnectionFactory` |
| `System.Diagnostics.ProcessExtensions` | `NexusLabs.Framework.Diagnostics.ProcessExtensions` |
| `System.Threading.Tasks.ActionExtensions` | `NexusLabs.Framework.Threading.Tasks.ActionExtensions` |
| `System.Threading.Tasks.AsyncVoidHelper` | `NexusLabs.Framework.Threading.Tasks.AsyncVoidHelper` |
| `System.Threading.Tasks.EventExtensions` | `NexusLabs.Framework.Threading.Tasks.EventExtensions` |
| `System.Threading.Tasks.GenericEventExtensions` | `NexusLabs.Framework.Threading.Tasks.GenericEventExtensions` |
| `System.Threading.Tasks.MulticastDelegateExtensions` | `NexusLabs.Framework.Threading.Tasks.MulticastDelegateExtensions` |
| `System.Threading.Tasks.TaskExtensions` | `NexusLabs.Framework.Threading.Tasks.TaskExtensions` |

### Deprecated

- `NexusLabs.Framework.ITimeProvider` and `NexusLabs.Framework.TimeProviderWrapper` are now marked `[Obsolete]`. They still ship in v1 but will be removed in the next major. Migrate to BCL `System.TimeProvider` (net8+); for tests use `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`).

### Fixed

- `StreamWithLength.Position` XML docs corrected. The 0.1.4 release notes claimed *"setting Position to zero would be a no-op"* was fixed but the doc comment still described the old guarded behavior. Docs now match the implementation (unconditional delegation to the wrapped stream).

### Repository infrastructure (no public-API impact)

- **License added**: MIT (`LICENSE` at repo root, `PackageLicenseExpression=MIT` in the nuspec).
- **README rewritten** from the BitBucket scaffold template into a real package README. The README is shipped inside the .nupkg as `PackageReadmeFile`.
- **SourceLink** enabled (`Microsoft.SourceLink.GitHub`). Commit hash and source paths are embedded so consumers can step into the source while debugging.
- **Symbol packages** (`.snupkg`) now produced alongside the main `.nupkg`.
- **Deterministic builds** under CI (`ContinuousIntegrationBuild=true` when `$(GITHUB_ACTIONS) == true`).
- **Central Package Management** enabled (`Directory.Packages.props`).
- **`TreatWarningsAsErrors=true`** repo-wide.
- **`Nullable=enable` + `ImplicitUsings=enable` + `LangVersion=latest`** repo-wide.
- **Repository layout** reorganized into `src/` + `tests/` with rename history preserved (`git log --follow` works).
- **Solution format** migrated from `.sln` to `.slnx`.
- **CI**: replaced CircleCI (which previously did only restore+build, no tests) with GitHub Actions (`.github/workflows/ci.yml`) running build, MTP tests, pack, and artifact upload.
- **Release**: new `.github/workflows/release.yml` triggered on `v*.*.*` tag push, using nuget.org **Trusted Publishing** via OIDC (`NuGet/login@v1`). No long-lived NuGet API key stored in the repo. External setup required before first tag push — see [docs/nuget-trusted-publishing-setup.md](docs/nuget-trusted-publishing-setup.md).
- **Dependabot** weekly updates for nuget + github-actions ecosystems.
- **Test runner**: standardized on xunit.v3 + Microsoft.Testing.Platform (`global.json` sets the runner; `Directory.Build.props` ships the common test-project wiring).
- **`.editorconfig`** mirrors the genesis seed conventions (Roslynator rules, file-scoped namespaces, naming conventions).

### Archived packages

The following sibling packages no longer ship from this repository. Their 0.x package IDs remain on nuget.org and are scheduled to be marked **Deprecated** on the nuget.org dashboard (see [docs/archived-packages/NUGET_DEPRECATION_CHECKLIST.md](docs/archived-packages/NUGET_DEPRECATION_CHECKLIST.md)). Source for each is on the `release/0.x` branch.

| Package | Replacement / Guidance |
|---|---|
| `NexusLabs.Autofac` | None — repo standardized on Needlr DI; Autofac wrapper unmaintained. |
| `NexusLabs.Collections.Generic` | Mostly BCL replacements: `Enumerable.Chunk`, `OfType`, `Random.Shared.GetItems`, `BitFaster.Caching` (was already recommended via `[Obsolete]`). |
| `NexusLabs.Contracts` | BCL: `ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrEmpty`, `ArgumentException.ThrowIfNullOrWhiteSpace`. |
| `NexusLabs.Dynamo` | Source generation displaces the runtime-dynamic-interface use case. |
| `NexusLabs.Reflection` | No drop-in; inline the small helper set you used. |
| `NexusLabs.Testing.Xunit` | Successor library (xunit.v3 + `extension(Assert)` + Framework result types) planned separately. |

Per-package details in [docs/archived-packages/](docs/archived-packages/README.md).

### Known issue

One test is skipped due to a pre-existing race in `MulticastDelegateExtensions` when `ordered=false` and `stopOnFirstError=true`. Two async handlers may produce a single-exception or AggregateException-of-two depending on scheduling. The fix requires re-architecting that path and is tracked separately. The skip is on:

```
GenericEventExtensionTests.InvokeAsync_UnorderedStopOnFirstErrorTrueBothAsync_AllExceptionsCaught
```

---

## [0.1.4] - 2025-12-05

Last release of the 0.x line.

- StreamWithLength fixes (the `Position = 0` no-op bug).
- Tests updated to xunit.v3.
