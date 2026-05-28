# Changelog

All notable changes to **NexusLabs.&#42;** packages in this repository are
documented in this file. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

Starting with **0.2.1**, all packages in this monorepo ship under **lockstep
versioning** &mdash; a single version number advances every package together
on each `vX.Y.Z` tag push, regardless of which packages structurally changed.
Bumping `<Version>` in `Directory.Build.props` is the single edit that
controls all package versions. The release workflow packs and pushes every
package at the same version; `--skip-duplicate` makes re-pushing an unchanged
package at the existing version a no-op.

Pre-lockstep history (Framework 0.1.x and Xunit.Assertions 0.1.0) is
preserved at the bottom of this file for reference.

---

## [Unreleased]

Connection-pool resilience pass: timeout-bounded lease acquisition and a typed
exhausted-pool exception, plus dogfooding our own `[TransfersOwnership]`
attribute and IDisposableAnalyzers across the solution. Ported from learnings
in a downstream MySQL outage post-mortem.

### NexusLabs.Framework

Added &mdash; bounded-budget primitives on `SemaphoreSlimExtensions`:

- `SemaphoreSlim.AcquireAsync(TimeSpan timeout, CancellationToken cancellationToken)` &mdash; waits up to `timeout` for a slot, throws `TimeoutException` if the budget elapses with no slot acquired. `cancellationToken` is required (no default) so the source-level disambiguation against the existing `AcquireAsync(CancellationToken)` overload is unambiguous.
- `SemaphoreSlim.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken)` &mdash; same wait semantics but returns `Task<AsyncSemaphoreLease?>` &mdash; `null` on timeout instead of throwing. Prefer this when a `null` branch is cheaper than catching `TimeoutException`.

Both overloads validate `timeout` (must be non-negative or `Timeout.InfiniteTimeSpan`), short-circuit `TimeSpan.Zero` to a non-blocking acquire, and propagate `OperationCanceledException` on cancellation without consuming a slot.

### NexusLabs.Data.Sql

Added:

- `ConnectionPoolExhaustedException` &mdash; new sealed exception type. Derives from `InvalidOperationException` so existing connection-management catch handlers stay compatible. Carries an `AcquisitionTimeout` property so operators can correlate logs with the configured cap.

Changed (**BREAKING**):

- `LeasedAsyncDbConnection` ctor signature is now `(IAsyncDbConnection inner, SemaphoreSlim leaseSemaphore, TimeSpan acquisitionTimeout)`. The 2-arg ctor is removed entirely. Callers must specify an explicit acquisition budget; pass `Timeout.InfiniteTimeSpan` to keep the previous wait-until-the-caller-cancels behaviour (not recommended in production paths &mdash; saturated pool + never-cancelled token previously hung forever).
- `AsyncDbDecoratorExtensions.WithLease(this IAsyncDbConnection, SemaphoreSlim)` &rarr; `WithLease(this IAsyncDbConnection, SemaphoreSlim, TimeSpan acquisitionTimeout)`. Same migration story as the ctor.
- `OpenAsync` on `LeasedAsyncDbConnection` called twice without an intervening `Close`/`DisposeAsync` now throws `InvalidOperationException` immediately (matching `System.Data` conventions) instead of acquiring a second slot and releasing the prior lease. Removes a non-obvious magic behaviour that would deadlock at pool-size-1.
- When the acquisition budget elapses on `OpenAsync`, the call now throws `ConnectionPoolExhaustedException` instead of hanging.

Migration:

```csharp
// Before
.WithLease(sem)

// After &mdash; explicit budget
.WithLease(sem, TimeSpan.FromSeconds(10))

// After &mdash; preserve old "wait forever" behaviour (NOT recommended)
.WithLease(sem, Timeout.InfiniteTimeSpan)
```

Internal &mdash; replaced 12 `[SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP007")]` annotations across the solution with our own `[TransfersOwnership]` attribute (suppressed via the bundled `TransfersOwnershipDisposeSuppressor`). Solution now dogfoods its own analyzers: every `src/` project except the analyzer assemblies references `NexusLabs.Framework.Analyzers` and `IDisposableAnalyzers`.

---

## [0.2.1] &mdash; 2026-05-24

First release under lockstep versioning. All six `NexusLabs.&#42;` packages now
ship at the same version number on every release. Four of the six are
brand-new package IDs in this release; they skip an independently-numbered
0.1.0 lifecycle and debut directly at the lockstep version.

Lockstep rationale: every package in this repo releases from the same
`v*.*.*` tag triggering the same workflow that pushes all `.nupkg` files.
The previous per-package versioning created a fictional independence that
forced consumers to reason about which Framework version pairs with which
Data.Sql version. Lockstep eliminates that.

### NexusLabs.Framework

Added &mdash; `Try` orchestration layer over `Safely`:

- `NexusLabs.Framework.Try` &mdash; higher-level wrappers around `Safely` that combine result-pattern callback execution with optional `ILogger` logging and `[CallerMemberName]` capture. Overloads cover both `Task`/sync paths, both `TriedEx<T>`/`TriedNullEx<T?>`, and convenience helpers like `Try.CombineErrors(...)` / `Try.CombineErrorsIfNeeded(...)` for AggregateException assembly and `Try.ToCompletionOrCanceledAsync(...)` for cooperative cancellation handling.
- `NexusLabs.Framework.Logging.LoggerCancellationExtensions.LogWarningIfNotCancellation(...)` &mdash; `ILogger` extension that demotes `OperationCanceledException`/`TaskCanceledException` to Debug while logging all other exceptions at Warning.

Added &mdash; `AsyncSemaphoreLease` concurrency primitive:

- `NexusLabs.Framework.Threading.AsyncSemaphoreLease` &mdash; disposable `IDisposable` lease over an externally-owned `SemaphoreSlim`. Acquired via the new `SemaphoreSlim.AcquireAsync(CancellationToken)` extension method, so callsites read `using var lease = await _semaphore.AcquireAsync(ct);` &mdash; the slot release is structurally bound to scope exit. `Dispose` is thread-safe and idempotent via `Interlocked.Exchange<int>` &mdash; concurrent or repeated disposal releases the underlying semaphore at most once. The lease does NOT own the semaphore; for a pool-cap pattern where over-release should fail fast, construct the semaphore as `new SemaphoreSlim(limit, limit)`.
- `NexusLabs.Framework.Threading.SemaphoreSlimExtensions.AcquireAsync(this SemaphoreSlim, CancellationToken)` &mdash; C# 14 `extension(SemaphoreSlim)` block; the single public entrypoint for acquiring a lease.

Added &mdash; `ITracer` / `Tracer` over `System.Diagnostics.ActivitySource`:

- `NexusLabs.Framework.Diagnostics.Tracing.ITracer` &mdash; interface with `WithTracing` / `WithTracingAsync` callback wrappers. Operation name defaults to `[CallerMemberName]`; pass `operationName:` explicitly to override.
- `NexusLabs.Framework.Diagnostics.Tracing.Tracer` &mdash; default implementation that wraps an externally-owned `ActivitySource`. Caller controls the source name (which is what shows up in observability tools) and lifetime.
- `Tracer.Default` &mdash; process-wide convenience instance. Initially backed by an `ActivitySource` named `"NexusLabs"`. Reconfigure at startup via `Tracer.SetDefaultSourceName("MyApp")` (common case) or `Tracer.SetDefault(customTracer)` (full substitution). Reads are lock-free via `Volatile.Read`; swaps replace the reference and previously-handed-out instances remain usable.
- Exists primarily so consumers can substitute a no-op tracer in tests via DI / mocking. Code that doesn't need substitutability can use `ActivitySource` directly.

New dependency: `Microsoft.Extensions.Logging.Abstractions 10.0.8`. Surfaces transitively to all `NexusLabs.Framework` consumers. Small (one assembly), widely deployed.

`Try`, `LoggerCancellationExtensions`, and `AsyncSemaphoreLease` are ported from internal NexusLabs reference code. `AsyncSemaphoreLease` was hardened during the port (interlocked release replaces a plain `bool` flag that could race under concurrent disposal).

Fixed:

- `Safely.GetResultOrFalse<T>(Func<T>)`, `Safely.GetResultOrFalseAsync<T>(Func<Task<T>>)`, `Safely.GetResultOrException<T>(Func<T>)`, `Safely.GetResultOrExceptionAsync<T>(Func<Task<T>>)` now detect a `null` callback return explicitly. Previously the implicit `Tried<T>`/`TriedEx<T>` conversion would throw `ArgumentNullException` inside the try, and the catch would forward that BCL-internal exception to `errorCallback`. Now `errorCallback` receives a clear `InvalidOperationException("Callback returned null. ... use Safely.GetResultNullOrExceptionAsync if null is a valid result for your callback.")`. Behavior change in 0.x is acceptable per semver; the new behavior is what callers always wanted.

### NexusLabs.Framework.Analyzers (new package, debuts at 0.2.1)

Initial release. Roslyn analyzers for codebase hygiene and correct use of
NexusLabs.Framework types. Test-specific analyzers ship separately in
`NexusLabs.Xunit.Assertions.Analyzers` (planned); data-layer analyzers ship
separately in `NexusLabs.Data.Sql.Analyzers` (planned).

Diagnostic ID prefix `NLF` (NexusLabs Framework). Opt out of any individual
rule per project via `dotnet_diagnostic.NLFxxxx.severity = none` in
`.editorconfig`.

Rules:

| ID      | Category | Severity | Summary |
|---------|----------|----------|---------|
| NLF0001 | Usage    | Warning  | Do not use `Console.Write` / `Console.WriteLine` / `Debug.Write` / `Debug.WriteLine` in library code. Route output through `ILogger` or a comparable injectable abstraction. |

Package shape:
- Targets `netstandard2.0` (required by Roslyn &mdash; analyzers run inside the C# compiler).
- Marked `<DevelopmentDependency>true</DevelopmentDependency>` so consumers do not get the netstandard2.0 dll as a compile reference.
- Roslyn 4.14.x baseline; consumers need a .NET SDK that bundles a compatible compiler (current modern SDKs all do).

### NexusLabs.Xunit.Assertions

Lockstep version bump only &mdash; no behavioral changes since the previous
0.1.0 release. The version jumps from `0.1.0` to `0.2.1` so that this package
aligns with the rest of the NexusLabs.* repo under lockstep versioning.
Future releases bump in step with the other packages.

### NexusLabs.Data.Sql (new package, debuts at 0.2.1)

Initial release. Provider-agnostic decorators and helpers that compose around the
`NexusLabs.Framework.Data.IAsyncDb*` interfaces.

Highlights:
- `LeasedAsyncDbConnection` &mdash; `IAsyncDbConnection` decorator that acquires a slot on an externally-supplied `SemaphoreSlim` (via the `SemaphoreSlim.AcquireAsync(CancellationToken)` extension method that lives alongside the `AsyncSemaphoreLease` primitive in `NexusLabs.Framework.Threading`) on `OpenAsync`, releases on `Close`/`Dispose`, releases on failed open, and releases the prior lease if `OpenAsync` is called twice without an intervening close &mdash; so double-open never leaks pool capacity. Construct the semaphore as `new SemaphoreSlim(limit, limit)` for fail-fast over-release safety.
- `OpenTrackingDecorator` + `OpenConnectionTracker` &mdash; runtime-opt-in diagnostics that record callstack + timestamp of every successful `OpenAsync`. Use to debug "all pool connections busy" scenarios. Not gated on `#if DEBUG`. Timestamps come from the caller-supplied `TimeProvider` for deterministic test control.
- `LoggingAsyncDbCommand` &mdash; `IAsyncDbCommand` decorator with `ILogger` integration. By default logs only metadata (operation + `CommandTextLength`); full `CommandText` is included only when `LoggingAsyncDbCommandOptions.IncludeCommandText = true`. Default log level is `Debug`; override via `LogLevel`.
- `PredicateAsyncDbConnectionFactory` &mdash; `IDbConnectionFactory` built from caller-supplied callbacks. `ConnectionString` is captured at construction time, so there is no sync-over-async on the getter.
- `AsyncDbDecoratorExtensions.WithLease(SemaphoreSlim)`, `.WithOpenTracking(tracker, timeProvider)`, `.WithLogging(...)` &mdash; fluent composition. Recommended ordering is `inner.WithLease(sem).WithOpenTracking(tracker, timeProvider)` so lease wait time is observable by the outer tracker or logger.

Requires .NET 10. New dependencies: `Microsoft.Extensions.Logging.Abstractions` (inherited from Framework).

This package extracts and hardens patterns previously embedded in
BrandGhost's internal `NexusLabs.Data.Sql.MySql` project. Lifecycle and
cancellation bugs from that codebase are fixed in this release: cancellation-honoring
lease wait (via the `SemaphoreSlim.AcquireAsync(CancellationToken)` extension),
token-based lease release (Close on a never-opened wrapper does not release
another caller's lease), lease release on failed open, lease release on Close,
idempotent disposal at both the decorator and lease level, no capacity leak
under double-open, and elimination of the sync-over-async getter on the
predicate factory.

### NexusLabs.Data.Sql.MySql (new package, debuts at 0.2.1)

Initial release. MySQL provider for `NexusLabs.Data.Sql` and the
`NexusLabs.Framework.Data.IAsyncDb*` interfaces. Built on top of
`MySql.Data 9.7.0` (Oracle).

Highlights:
- `MySqlConnectionFactory` &mdash; public sealed `IDbConnectionFactory`. Builds the connection string via `MySqlConnectionStringBuilder`, so passwords containing reserved characters (`;`, `'`, `"`, `{`, `}`) survive intact. Validates `Server`/`Username`/`Password`/`Port`/`SslMode` at construction time. Disposes the connection on failed open or pre-cancellation. Wraps unexpected open failures in a clear `InvalidOperationException`.
- `IMySqlConnectionConfiguration` + `MySqlConnectionConfiguration` record &mdash; public configuration surface with sensible defaults (`MinimumPoolSize=1`, `MaximumPoolSize=50`, `SslMode="Preferred"`, `ConnectionLifeTime=300`).
- `AsyncMySqlConnection` / `AsyncMySqlCommand` / `AsyncMySqlDataReader` &mdash; internal `sealed` adapters. Every async overload delegates directly to the underlying `MySqlConnection`/`MySqlCommand`/`MySqlDataReader` async method &mdash; not through a `DbCommand`/`DbDataReader` base that would fall through to sync-over-async. This fixes a class of latent perf bugs from the BrandGhost source where async paths silently blocked threads.

Composition example:

```csharp
using var sem = new SemaphoreSlim(50, 50);
var tracker = new OpenConnectionTracker();
var factory = new MySqlConnectionFactory(config);

await using var conn = (await factory.OpenNewConnectionAsync(ct))
    .WithLease(sem, TimeSpan.FromSeconds(10))
    .WithOpenTracking(tracker, TimeProvider.System);

await using var cmd = conn.CreateAsyncCommand().WithLogging(logger);
cmd.CommandText = "SELECT 1";
var n = await cmd.ExecuteScalarAsync(ct);
```

The MySqlConnector swap is deferred; the adapter surface is `internal sealed`
so a future provider swap is a non-breaking change.

Requires .NET 10. New dependency: `MySql.Data 9.7.0`.

### NexusLabs.CodeAnalysis.Testing.TUnit (new package, debuts at 0.2.1)

Initial release. TUnit-flavored `IVerifier` implementation for
`Microsoft.CodeAnalysis.Testing`. Fills a gap in the Roslyn SDK ecosystem:
Microsoft ships verifiers for xUnit (`Microsoft.CodeAnalysis.Testing.Verifiers.XUnit`),
NUnit, and MSTest, but not for TUnit. Without this, TUnit-based test projects
cannot use the full `CSharpAnalyzerTest<TAnalyzer, TVerifier>` harness and
must fall back to ad-hoc `SupportedDiagnostics`-only sanity tests.

Highlights:
- `NexusLabs.CodeAnalysis.Testing.TUnit.TUnitVerifier` &mdash; `public class : DefaultVerifier`. Overrides every assertion method to throw `TUnit.Assertions.Exceptions.AssertionException` on failure. Reuses the base `CreateMessage` helper so context pushed via `PushContext` is still prefixed onto failure messages.
- Usable as `CSharpAnalyzerTest<MyAnalyzer, TUnitVerifier>` (or any of the other `...<TVerifier>` harness types) from a TUnit-based test project.

Usage:

```csharp
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NexusLabs.CodeAnalysis.Testing.TUnit;

[Test]
public async Task MyAnalyzer_FlagsBadCode()
{
    var test = new CSharpAnalyzerTest<MyAnalyzer, TUnitVerifier>
    {
        TestCode = """
            public class C
            {
                public void M() => {|#0:BadApi()|};
            }
            """,
    };

    test.ExpectedDiagnostics.Add(
        new DiagnosticResult(MyAnalyzer.Rule).WithLocation(0));

    await test.RunAsync();
}
```

Requires .NET 10. New dependencies:
- `Microsoft.CodeAnalysis.Analyzer.Testing 1.1.2` (provides `IVerifier` + `DefaultVerifier`)
- `Microsoft.CodeAnalysis.Common 4.14.0` and `Microsoft.CodeAnalysis.CSharp.Workspaces 4.14.0` (explicit overrides to prevent the netfx-only `1.0.1` transitive that triggers NU1701)
- `TUnit.Assertions 1.45.29` (provides `AssertionException`)

Genesis follow-up: the `roslyn-tooling` template currently ships a
SupportedDiagnostics-only sanity test because of the missing TUnit
verifier. With this package available, the template can be updated to
consume `NexusLabs.CodeAnalysis.Testing.TUnit` and use the full harness.
That update is tracked separately.

---

## Pre-lockstep history

The sections below predate lockstep versioning. Packages were released
independently, each with its own version timeline.

### NexusLabs.Framework [0.2.0] &mdash; 2026-05-23

Major content overhaul, but the package stays in `0.x` because several
things in this surface are still in flux (deprecated `ITimeProvider`, a
pre-existing null-handling gap in `Safely.*`, a known race in
`MulticastDelegateExtensions`). `1.0.0` is reserved for when the API is
committed-to-stable.

Single surviving package (`NexusLabs.Framework`); six sibling packages
archived (see "Archived packages" below). Every change in this release is
intentional and was inventoried via `dotnet apicompat` against
`NexusLabs.Framework 0.1.4` &mdash; see [docs/v0.2-breaking-changes.md](docs/v0.2-breaking-changes.md)
for the full inventory and per-type replacement guidance.

#### Removed (Breaking)

The following public types were removed from the package. The 0.x line
remains on nuget.org if you need them; archived source is on the
`release/0.x` branch.

- `NexusLabs.Framework.Cast` / `NexusLabs.Framework.ICast` &mdash; reflection-based magic-cast. Use `Convert.ChangeType`, generic math, or write the conversion you need at the callsite.
- `NexusLabs.Framework.OnlyOnce` / `NexusLabs.Framework.OnlyOnce<T>` / `NexusLabs.Framework.IOnlyOnce` &mdash; `Lazy<T>` does this trivially.
- `System.StringExtensions.ToStream(Encoding)` &mdash; inline at callsite: `new MemoryStream(encoding.GetBytes(str))`.
- `NexusLabs.Framework.IO.BlockingBufferStream` &mdash; use `System.IO.Pipelines` (`PipeReader`/`PipeWriter`).
- `System.Data.IAsyncDbDataReaderExtensions` (326 lines of `GetXAsync`/`GetXOrNullAsync` overloads) &mdash; use BCL `DbDataReader.GetFieldValueAsync<T>` + `IsDBNullAsync`.
- `System.Data.IDataReaderExtensions`, `System.Data.IDBCommandExtensions`, `System.Data.Common.DbDataReaderExtensions` &mdash; inline at consumer callsites.
- `NexusLabs.Framework.Data.PredicateMySqlConnectionFactory` &mdash; duplicate of a consumer-owned class.

#### Changed (Breaking) &mdash; namespace pollution cleanup

The following types moved out of BCL namespaces. Consumers must add a new
`using NexusLabs.Framework.*;` directive in every file that uses these
types. Compile errors will pinpoint each callsite.

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

#### Deprecated

- `NexusLabs.Framework.ITimeProvider` and `NexusLabs.Framework.TimeProviderWrapper` are now marked `[Obsolete]`. They still ship in 0.2 but are scheduled for removal in a future 0.x release (before 1.0). Migrate to BCL `System.TimeProvider` (net8+); for tests use `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`).

#### Fixed

- `StreamWithLength.Position` XML docs corrected. The 0.1.4 release notes claimed *"setting Position to zero would be a no-op"* was fixed but the doc comment still described the old guarded behavior. Docs now match the implementation (unconditional delegation to the wrapped stream).

#### Repository infrastructure (no public-API impact)

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
- **Release**: new `.github/workflows/release.yml` triggered on `v*.*.*` tag push, using nuget.org **Trusted Publishing** via OIDC (`NuGet/login@v1`). No long-lived NuGet API key stored in the repo. External setup required before first tag push &mdash; see [docs/nuget-trusted-publishing-setup.md](docs/nuget-trusted-publishing-setup.md).
- **Dependabot** weekly updates for nuget + github-actions ecosystems.
- **Test runner**: standardized on xunit.v3 + Microsoft.Testing.Platform (`global.json` sets the runner; `Directory.Build.props` ships the common test-project wiring).
- **`.editorconfig`** mirrors the genesis seed conventions (Roslynator rules, file-scoped namespaces, naming conventions).

#### Archived packages

The following sibling packages no longer ship from this repository. Their
0.x package IDs remain on nuget.org and are scheduled to be marked
**Deprecated** on the nuget.org dashboard (see
[docs/archived-packages/NUGET_DEPRECATION_CHECKLIST.md](docs/archived-packages/NUGET_DEPRECATION_CHECKLIST.md)).
Source for each is on the `release/0.x` branch.

| Package | Replacement / Guidance |
|---|---|
| `NexusLabs.Autofac` | None &mdash; repo standardized on Needlr DI; Autofac wrapper unmaintained. |
| `NexusLabs.Collections.Generic` | Mostly BCL replacements: `Enumerable.Chunk`, `OfType`, `Random.Shared.GetItems`, `BitFaster.Caching` (was already recommended via `[Obsolete]`). |
| `NexusLabs.Contracts` | BCL: `ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrEmpty`, `ArgumentException.ThrowIfNullOrWhiteSpace`. |
| `NexusLabs.Dynamo` | Source generation displaces the runtime-dynamic-interface use case. |
| `NexusLabs.Reflection` | No drop-in; inline the small helper set you used. |
| `NexusLabs.Testing.Xunit` | Successor library (xunit.v3 + `extension(Assert)` + Framework result types) planned separately. |

Per-package details in [docs/archived-packages/](docs/archived-packages/README.md).

#### Known issue

One test is skipped due to a pre-existing race in
`MulticastDelegateExtensions` when `ordered=false` and
`stopOnFirstError=true`. Two async handlers may produce a single-exception
or AggregateException-of-two depending on scheduling. The fix requires
re-architecting that path and is tracked separately. The skip is on:

```
GenericEventExtensionTests.InvokeAsync_UnorderedStopOnFirstErrorTrueBothAsync_AllExceptionsCaught
```

### NexusLabs.Xunit.Assertions [0.1.0] &mdash; 2026-05-23

Initial release. xUnit.v3 assertion augmentations that integrate with the
NexusLabs.Framework result-pattern types (`TriedEx<T>`, `TriedNullEx<T?>`,
`ExceptionHelper`) and HTTP response shapes. Uses C# 14 `extension(Assert)`
blocks so helpers are callable as `Assert.TrySucceeded(...)`,
`Assert.HttpRequestHasResponse<T>(...)`, etc. &mdash; same shape as built-in xUnit
assertions.

Successor to the deprecated `NexusLabs.Testing.Xunit` 0.x line (see
[docs/archived-packages/NexusLabs.Testing.Xunit.md](docs/archived-packages/NexusLabs.Testing.Xunit.md)).
Different package ID; not a drop-in replacement.

Highlights:
- `Assert.TrySucceeded<T>(TriedEx<T>, ...)` / `Assert.TryFailed<T, TException>(...)` for assertions over result-pattern values.
- `Assert.HttpRequestHasResponse<T>(...)` / `Assert.HttpRequestFailed<T>(...)` / `Assert.HttpSuccess(HttpResponseMessage)` / `Assert.HttpNotOk(...)` for HTTP integration tests.
- Lazy-message overloads of `Assert.True/False/NotNull/NotEmpty` (Func<string>) that allocate the message only on failure.
- `Assert.EqualIgnoreLineEndingStyle(...)` for cross-platform string equality.
- `Assert.GreaterThan/GreaterThanOrEqual/LessThan/LessThanOrEqual/GreaterThanZero/InRange<T>(...)` numeric comparison helpers.

Requires .NET 10 and xUnit.v3 3.x. References `xunit.v3.extensibility.core` +
`xunit.v3.assert` (NOT `xunit.v3` itself &mdash; that would mark consumers as test
runners).

### NexusLabs.Framework [0.1.4] &mdash; 2025-12-05

Last release of the pre-0.2.0 line.

- StreamWithLength fixes (the `Position = 0` no-op bug).
- Tests updated to xunit.v3.
