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

## [0.2.9] &mdash; 2026-08-01

Adds the `NexusLabs.Testing` package and four `TimeProvider` analyzers. Per
lockstep versioning, every `NexusLabs.*` package advances to 0.2.9 together;
`NexusLabs.Testing` enters the line at that version.

### NexusLabs.Testing

New package. Framework-agnostic test helpers. The initial surface is the `Time`
namespace; further concepts get their own namespace and folder here, and split
into a dedicated package only if one of them drags in dependencies the rest
should not carry.

Added:

- `RegistrationObservingTimeProvider`, a `FakeTimeProvider` that reports when
  the code under test arms a timer. `FakeTimeProvider.Advance` only fires
  timers that are already registered, so a test whose advance lands before the
  registration leaves the timer due at a simulated time it never reaches and
  the awaited work hangs. `WaitForArmedTimersAsync` waits for the registration
  itself, which removes the race instead of racing it faster. Every override
  delegates to the base scheduler, so virtual time keeps applying.
- `FakeTimeProviderWaitExtensions.AdvanceUntilAsync`, a pump for when the
  expected registration count is not knowable. Bounded on real time and on
  total injected simulated time, so it cannot spin indefinitely or move the
  clock by simulated years and fire unrelated long-horizon timers. It converges
  rather than synchronising and can still lose the race under load, so
  `WaitForArmedTimersAsync` is preferred whenever the count is known.
- `Wait.UntilAsync`, the framework-agnostic condition wait underneath both. Its
  deadline is measured against `TimeProvider.System` and is deliberately not
  configurable: a caller could otherwise supply the clock being pumped, and
  each advance would consume its own deadline budget.
- `WaitOutcome`, returned by all three. A failed wait, an exhausted budget and
  a cancelled token are results rather than exceptions, so the primitives sit
  below any particular test framework. The failure text carries the predicate
  source, captured through `CallerArgumentExpression`.

### NexusLabs.Framework.Analyzers

Added:

- **NLF0025** &mdash; a call or construction that has a `TimeProvider` overload,
  made where a clock is already reachable but was not passed. Covers both
  invocations (`Task.Delay`) and object creations
  (`new CancellationTokenSource(delay)`, `new PeriodicTimer(period)`). Static
  members are excluded so the inherited `TimeProvider.System` is never offered
  as the available clock.
- **NLF0026** &mdash; the NLF0025 companion for a call site with no reachable
  clock. Ships at Info because the fix is an API change rather than a defect in
  the statement. The two rules are mutually exclusive.
- **NLF0027** &mdash; an interface that reimplements `TimeProvider`. Reports the
  common clock-plus-delay shape rather than exempting it; a broader interface
  that merely exposes a timestamp is not reported.
- **NLF0028** &mdash; a `FakeTimeProvider` subclass whose `CreateTimer` override
  never calls `base.CreateTimer`. Replacing the timer disables virtual time for
  everything the test drives through that clock, so delays complete without the
  clock moving and tests pass without exercising them. Overrides that delegate
  to the base are not reported.

### NexusLabs.Framework

Changed:

- The obsolete `ITimeProvider` now suppresses NLF0027 in place. It is the exact
  shape that rule exists to prevent and is kept only until the next major
  version, so the suppression documents the exception rather than weakening the
  rule.

## [0.2.8] &mdash; 2026-07-27

Patch release restoring Dapper asynchronous compatibility for the MySQL data
package. Per lockstep versioning, every `NexusLabs.*` package advances to
0.2.8 together.

### NexusLabs.Data.Sql.MySql

Fixed:

- Restored standard ADO.NET runtime compatibility for the internal MySQL
  adapters. Connections now preserve `DbConnection` identity and commands
  preserve `DbCommand` identity while continuing to implement the
  `IAsyncDbConnection` / `IAsyncDbCommand` contracts. Dapper 2.1.66 async
  execution can therefore use factory-created connections without rejecting
  the adapter types.
- Provider-native asynchronous execution, cancellation, reader creation,
  transaction/parameter behavior, and disposal remain directly delegated to
  `MySqlConnection` and `MySqlCommand`; the fix does not introduce
  sync-over-async fallback behavior.
- Preserved the provider's existing parameterless transaction-isolation
  behavior across both `IDbConnection` and `DbConnection` dispatch paths.

### Build and testing

Changed:

- Added Dapper 2.1.66 as a test-only dependency and regression coverage for
  `ExecuteAsync`, `QueryAsync`, and async reader setup. Dapper is not a runtime
  dependency of any published package.
- Updated `Microsoft.Extensions.Logging.Abstractions` to 10.0.10,
  `Microsoft.SourceLink.GitHub` to 10.0.301,
  `Microsoft.Extensions.TimeProvider.Testing` to 10.8.0,
  `Microsoft.NET.Test.Sdk` to 18.8.1, and TUnit / TUnit.Assertions to 1.61.38.

## [0.2.7] &mdash; 2026-07-24

Release adding two new packages: TUnit-native assertions for the
`NexusLabs.Framework` result types and additive UUIDv7 creation for GUID-backed
strongly typed identifiers. Per lockstep versioning, every `NexusLabs.*`
package advances to 0.2.7 together.

### NexusLabs.StronglyTypedIds (new package)

Added an additive UUIDv7 creation layer for GUID-backed strongly typed
identifiers:

- `GuidIdTemplates.UuidV7` selects the packaged template while retaining the
  existing GUID-backed parsing, formatting, equality, serialization, and
  conversion surface. Its IntelliSense documentation explicitly warns that the
  GUID constructor accepts arbitrary values and therefore does not establish a
  UUIDv7 invariant.
- Generated `Create()` and `Create(TimeProvider)` methods produce RFC 9562
  UUIDv7 values. The explicit `TimeProvider` overload supports deterministic
  timestamp tests; the parameterless overload uses `TimeProvider.System`.
- `IUuidV7IdentifierGenerator<TIdentifier>` and
  `UuidV7IdentifierGenerator<TIdentifier>` provide a mockable generic creation
  seam. `AddUuidV7IdentifierGeneration()` registers the open generic and adds
  `TimeProvider.System` only when the application has not supplied another
  provider.
- **NLS0001** reports the built-in parameterless `New()` method because it
  creates UUIDv4 values and directs callers to `Create()`.
- **NLS0002** reports direct construction from `Guid.NewGuid()` while preserving
  arbitrary GUID construction for persistence and deserialization.
- Added installed-package verification for template delivery, transitive source
  generator availability, analyzer delivery, and packaged IntelliSense XML
  documentation.
- The source-generator dependency is pinned exactly to `1.0.0-beta08`; package
  build assets forward its generator assemblies because analyzer assets do not
  otherwise flow through a transitive NuGet dependency.

### NexusLabs.TUnit.Assertions (new package)

Added TUnit-native assertions for `NexusLabs.Framework` result types:

- `Assert.That(result).Succeeded()` validates a successful `TriedEx<T>` or
  `TriedNullEx<T>` and returns its value from `await`. `TriedEx<T>` preserves
  its non-null value contract; `TriedNullEx<T>` preserves nullable success.
- `Assert.That(result).Failed()` validates failure and returns the original
  captured `Exception`. Chain `.With<TException>()` to require an assignable
  exception type and receive the same exception instance strongly typed.
- TUnit's `.Because(...)` context is preserved in assertion failures, and
  failures retain the original captured exception as the inner exception.
- **NLT0001** ships inside the same NuGet package and reports
  `Assert.That(result.Success)`, `Assert.That(result.Value)`, and
  `Assert.That(result.Error)`, directing callers to assert the complete result
  with `Succeeded()` or `Failed()`.

### NexusLabs.Xunit.Assertions

- Refactored the existing `TrySucceeded` and `TryFailed` helpers to share one
  framework-neutral Tried-result evaluator with the TUnit package. Public API,
  return values, exception identity, and failure-message behavior are
  unchanged.

### Build and testing

- Added first-class TUnit test-project support alongside the existing xUnit
  default.
- Added a post-pack consumer smoke test that installs the generated
  `NexusLabs.TUnit.Assertions` package, runs its assertions under TUnit, and
  verifies the bundled analyzer reports NLT0001.
- Updated `TUnit` / `TUnit.Assertions` to 1.61.15.

---

## [0.2.6] &mdash; 2026-07-10

Release adding `ArrayPool<T>` renting handles to `NexusLabs.Framework` and the NLF0024 analyzer that
guards their use. Per lockstep versioning every package advances to 0.2.6 together; only
`NexusLabs.Framework` and `NexusLabs.Framework.Analyzers` changed structurally, and the new types add
no third-party dependency.

### NexusLabs.Framework

Added &mdash; `ArrayPool<T>` renting handles (`NexusLabs.Framework.Buffers`), a safe pair of
scope-bound handles that replace the manual `Rent`/`try`/`finally`/`Return` boilerplate with a
single `using`:

- `RentedSpan<T>` &mdash; a zero-allocation, synchronous `ref struct` handle. The compiler
  guarantees it can never escape to the heap (no boxing, fields, lambda capture, collection storage,
  or crossing `await`/`yield`) — those are compile errors — which rules out use-after-return bugs by
  construction. Allocates nothing (verified: 0 bytes/op). Exposes the raw `Array` (for `T[]`-shaped
  APIs), length-bounded `Span`, `AsSpan` windows, an in-place indexer, `Length` (requested) and
  `Capacity` (granted). Idempotent disposal; move-only.
- `RentedMemory<T>` &mdash; the reference-type owner for buffers that must be held across `await`.
  Implements `IMemoryOwner<T>`. Because it is a class, assigning/passing/capturing it copies a
  *reference* to one shared owner, so the array is returned exactly once no matter how many copies
  exist — copy-safe by construction, with a thread-safe idempotent `Dispose`. Exposes `Memory`,
  `Span`, `Array`, `AsArraySegment`, `AsMemory`/`AsSpan` windows, an indexer, `Length` and
  `Capacity`. The trade-off versus `RentedSpan<T>` is one small heap allocation for the owner (the
  rented array itself is still pooled).
- `ArrayPoolExtensions.RentSpan` / `RentMemory` &mdash; acquisition extensions shaped like BCL
  instance members (`pool.RentSpan(minimumLength)`, `pool.RentMemory(minimumLength)`), mirroring the
  `AsyncSemaphoreLease` acquire-then-`using` pattern. Set `clearOnReturn: true` to wipe buffers that
  held sensitive data before they return to the pool.

These types add no third-party dependency.

### NexusLabs.Framework.Analyzers

- **NLF0024** &mdash; flags copying a `RentedSpan<T>` handle: an assignment source, a variable
  initializer (including `using var b = handle;`), a by-value argument, a ternary/switch-expression
  branch, returning a `using`-bound handle, or a silent defensive copy from invoking a
  non-`readonly` member through an `in`/`ref readonly` handle. Copying a single-owner `ref struct`
  creates a second owner of the same rented array, so disposing both copies double-returns it and
  corrupts the pool. Operation-based, so member-access receivers, `ref`/`in`/`out` arguments,
  discards, `is` patterns, `nameof`, fresh `RentSpan` acquisitions, and bare `return` moves are not
  flagged; the reference-type `RentedMemory<T>` is not analyzed (copy-safe by construction). See
  `docs/analyzers/NLF0024.md`.

---

## [0.2.5] &mdash; 2026-06-17

Release adding self-deleting temporary file and directory primitives to `NexusLabs.Framework`.
Per lockstep versioning every package advances to 0.2.5 together; only `NexusLabs.Framework`
changed structurally, and the new types add no third-party dependency.

### NexusLabs.Framework

Added &mdash; temporary file and directory primitives (`NexusLabs.Framework.IO`):

- `ITemporaryDirectory` / `ITemporaryFile` &mdash; `IDisposable` + `IAsyncDisposable`
  handles that delete themselves (and, for directories, their contents) on disposal.
  Use them with `using` / `await using`, mirroring the `AsyncSemaphoreLease` pattern.
- `ITemporaryDirectoryFactory` / `ITemporaryFileFactory` (plus the default
  `TemporaryDirectoryFactory` / `TemporaryFileFactory`) &mdash; the mockable creation seam so
  consumer code can be unit tested without touching the real file system.
- `TemporaryDirectoryOptions` / `TemporaryFileOptions` &mdash; configure the root location, name
  prefix, file extension / pre-creation, cleanup-failure handling (`OnCleanupError`), and an
  optional `DeleteExecutor` resilience policy, all at creation time.
- `ResilientDeleteExecutor` &mdash; a delegate whose signature matches the common
  `Execute(operation, cancellationToken)` shape of resilience pipelines, so a caller drops their
  retry/backoff policy in as a method group (`DeleteExecutor = myResiliencePolicy.ExecuteAsync`)
  with no adapter and **no third-party dependency in the package**. Deletion clears read-only
  attributes, is reparse-point-safe, treats an already-deleted resource as success, never throws
  from `Dispose`, and routes the final failure to `OnCleanupError` (or the factory's logger) rather
  than silently giving up.

---

## [0.2.4] &mdash; 2026-06-17

Release covering four new analyzer rules in `NexusLabs.Framework.Analyzers`
&mdash; one async-cancellation rule (NLF0020) and three Moq-discipline rules
(NLF0021 through NLF0023) &mdash; plus the dog-fooded enforcement they
required across the repo's own sources and test suites. `NexusLabs.Framework`
and `NexusLabs.Data.Sql.MySql` gain a cancellable
`IAsyncDbDataReader.CloseAsync(CancellationToken)` overload (a breaking
interface addition for external implementers; existing callers are
unaffected). Per pre-1.0 versioning, breaking changes ship as a patch
increment.

### NexusLabs.Framework.Analyzers

Four new rules. The package now ships **twenty-three** diagnostics plus one
diagnostic suppressor (NLFSUP001). No structural changes to the package
shape, the `netstandard2.0` target, the
`<DevelopmentDependency>true</DevelopmentDependency>` declaration, or the
dual-assembly `Analyzers.dll` / `CodeFixes.dll` layout.

Added &mdash; analyzer rules:

| ID      | Category | Severity | Summary |
|---------|----------|----------|---------|
| NLF0020 | Usage    | Warning  | A method that is async (carries the `async` keyword OR whose name ends with the `Async` suffix) must declare a `System.Threading.CancellationToken` parameter so the operation can be cancelled. Enforces token PRESENCE only; `CA1068` owns last-position, so the two compose without overlapping. Exempts overrides, interface implementations, `async void` event handlers (`(object, EventArgs)`), `[Fact]`/`[Theory]`/`[Test]`/`[TestMethod]` test methods, `Main`, same-named sibling overloads that already take a token (the BCL convenience-overload pattern, e.g. `ExecuteNonQueryAsync()` beside `ExecuteNonQueryAsync(CancellationToken)`), and methods accepting a delegate-typed parameter (`Func`/`Action`/`EventHandler`, a custom delegate, or the base `System.Delegate` / `System.MulticastDelegate` &mdash; cancellation belongs to the caller-supplied callback). Suppress per-method with `#pragma warning disable NLF0020` for a genuinely uncancellable async method. |
| NLF0021 | Usage    | Warning  | Moq mocks must be created from a shared `MockRepository` (`_mocks.Create<T>()`), not via `new Mock<T>(...)` or `Mock.Of<T>(...)`. A shared repository gives every mock one strict behavior and lets `VerifyAll()` assert all setups in one place. Matches `Moq.Mock<T>` and `Moq.Mock.Of` by namespace + name. |
| NLF0022 | Usage    | Warning  | Moq mocks must use `MockBehavior.Strict` so unconfigured calls fail fast instead of returning silent defaults. Fires on a `Moq.MockRepository` constructed with a non-Strict behavior and on a `repository.Create<T>(MockBehavior.Loose/Default, ...)` override. Direct `new Mock<T>(...)` is owned by NLF0021 and not double-flagged. |
| NLF0023 | Usage    | Warning  | `It.IsAny<T>()` where `T` is a value type (other than `CancellationToken`) or a record hides the value the code under test actually passed. Match the expected value directly or use `It.Is<T>(x => ...)`. `CancellationToken` is exempt (the one value type routinely matched by type, not value); reference types and open generic type parameters are unaffected. |

### NexusLabs.Framework

Changed (**BREAKING**) &mdash; `IAsyncDbDataReader` gains a cancellable close:

- **`IAsyncDbDataReader.CloseAsync(CancellationToken cancellationToken)`** added alongside the existing parameterless `CloseAsync()`. The parameterless overload remains and is now the NLF0020 sibling-exempt convenience entry point. External implementers of `IAsyncDbDataReader` must add the new overload in lockstep; external callers are unaffected (the parameterless overload still exists).

### NexusLabs.Data.Sql.MySql

Added &mdash; follow-through for the `IAsyncDbDataReader` signature change:

- `AsyncMySqlDataReader.CloseAsync(CancellationToken cancellationToken)` implemented. The underlying `MySqlDataReader.CloseAsync()` exposes no token overload, so the adapter honors **pre-cancellation** only &mdash; it returns `Task.FromCanceled` when the token is already cancelled, otherwise delegates to the provider's parameterless close.

### NexusLabs.Data.Sql

Lockstep version bump only &mdash; no behavioural changes since `0.2.3`.

### NexusLabs.Xunit.Assertions

Lockstep version bump only &mdash; no behavioural changes since `0.2.3`.

### NexusLabs.CodeAnalysis.Testing.TUnit

Lockstep version bump only &mdash; no behavioural changes since `0.2.3`.

---

## [0.2.3] &mdash; 2026-06-09

Release covering seven new analyzer rules (NLF0013 through NLF0019) in
`NexusLabs.Framework.Analyzers`, a new `AsyncGate` async manual-reset
primitive in `NexusLabs.Framework`, and the dog-fooded breaking changes
the new analyzer strictness surfaced in `NexusLabs.Framework`,
`NexusLabs.Data.Sql`, and `NexusLabs.Data.Sql.MySql`. `NexusLabs.Xunit.Assertions` gains per-
method `#pragma` suppressions on its `TriedEx`/`TriedNullEx`-aware
assertion helpers — no behavioural change there, just an explicit
opt-out from the new NLF0015 rule that the helpers structurally
contradict (their job is to consume Try-results, not produce them).

### NexusLabs.Framework

Added &mdash; async coordination primitive:

- **`AsyncGate`** (`NexusLabs.Framework.Threading`) &mdash; a sealed, `IDisposable` async manual-reset gate: an awaitable signal callers park on via `WaitAsync(CancellationToken)` until `Set()` opens it (releasing every current and future waiter until `Reset()`). Mirrors `ManualResetEventSlim` (`Set`/`Reset`/`IsSet`) but exposes an awaitable wait, so no thread-pool thread is held while parked; built on a `TaskCompletionSource` created with `RunContinuationsAsynchronously` so a waiter's continuation never runs inline on the thread that calls `Set()`. `Set()` is the release; `Dispose()` is scope-exit teardown that **cancels** any still-parked waiter (they observe `OperationCanceledException`, never a normal completion) &mdash; the inverse of a resource lease such as `AsyncSemaphoreLease`, where disposal is the release.

Changed (**BREAKING**) &mdash; `SemaphoreSlim` extension rename:

- `SemaphoreSlim.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken)` &rarr; **`SemaphoreSlim.AcquireOrNullAsync(TimeSpan timeout, CancellationToken cancellationToken)`**. Shipped in `0.2.2` as `TryAcquireAsync` returning `Task<AsyncSemaphoreLease?>` (null on timeout). The `Try` prefix in this codebase contractually means "returns `TriedEx<T>` / `TriedNullEx<T>` / `Exception?`" &mdash; not "returns `T?`". Renaming aligns the API with the new **NLF0015** rule (Try-prefixed methods must return a Try-result type). Migration: replace `sem.TryAcquireAsync(timeout, ct)` with `sem.AcquireOrNullAsync(timeout, ct)`. Semantics, return type, validation, and cancellation behavior are unchanged &mdash; this is a rename only.

Changed (**BREAKING**) &mdash; `CancellationToken` defaults removed from public-API extensions:

- `Process.WaitForExitAsync(Action<Process>, CancellationToken cancellationToken)` &mdash; `= default` removed.
- `Process.StartAndWaitForExitAsync(ProcessStartInfo, CancellationToken cancellationToken, Action<Process>? afterStartCallback = null)` &mdash; `= default` removed AND parameter order reordered. The `CancellationToken` now precedes the optional `afterStartCallback` because C# disallows required-after-optional. Callers that previously passed `afterStartCallback` positionally (e.g. `proc.StartAndWaitForExitAsync(psi, p => ...)`) now get a compile error and must pass the token explicitly: `proc.StartAndWaitForExitAsync(psi, ct, p => ...)`.

These changes align the public surface with the new **NLF0018** rule (`CancellationToken` parameters must not have a default value &mdash; optional tokens let callers silently drop cancellation). Migration is mechanical: pass `CancellationToken.None` explicitly at any call site that previously relied on the default.

Changed (**BREAKING**) &mdash; `IDbConnectionFactory` / `IAsyncDbDataReader` interface signatures:

- `IDbConnectionFactory.CreateNewConnectionAsync(CancellationToken cancellationToken)` &mdash; `= default` removed.
- `IDbConnectionFactory.OpenNewConnectionAsync(CancellationToken cancellationToken)` &mdash; `= default` removed.
- `IAsyncDbDataReader.ReadAsync(CancellationToken cancellationToken)` &mdash; `= default` removed.

External implementers of these interfaces must update their signatures in lockstep. External callers must pass a `CancellationToken` explicitly &mdash; pass `CancellationToken.None` at sites that genuinely have no token to thread (this is the point of the rule: make the choice visible in code instead of hiding it behind an omitted argument).

Internal &mdash; documented NLF0018 exceptions:

- `SemaphoreSlim.AcquireAsync(CancellationToken cancellationToken = default)` keeps its default and is suppressed with `#pragma warning disable NLF0018` plus inline rationale: it is the ergonomic-default companion to `AcquireAsync(TimeSpan, CancellationToken)` (a top-level entrypoint for callers that have no token to thread). The mandatory-CT overload is still available for callers that want explicit cancellation.
- `TaskExtensions.ToUnorderedAsyncEnumerable<TSource, TResult>(..., [EnumeratorCancellation] CancellationToken cancellationToken = default)` keeps its default and is suppressed with `#pragma warning disable NLF0018`. The BCL `[EnumeratorCancellation]` convention requires the default so that `await foreach (var x in source.Iter())` works and `WithCancellation(token)` can flow the consumer's token through the attribute.
- `TaskExtensions.ToOrderedAsyncEnumerable<TSource, TResult>(..., [EnumeratorCancellation] CancellationToken cancellationToken = default)` &mdash; same rationale as the unordered variant.

Each suppression names its rationale inline and references `docs/analyzers/NLF0018.md` for the canonical exception list.

### NexusLabs.Framework.Analyzers

Seven new rules. The package now ships **nineteen** diagnostics plus one
diagnostic suppressor (NLFSUP001). No structural changes to the
package shape, the `netstandard2.0` target, the
`<DevelopmentDependency>true</DevelopmentDependency>` declaration, or
the dual-assembly `Analyzers.dll` / `CodeFixes.dll` layout introduced
in `0.2.2`.

Added &mdash; analyzer rules:

| ID      | Category | Severity | Summary |
|---------|----------|----------|---------|
| NLF0013 | Usage    | Warning  | Use a `[StronglyTypedId]`-decorated ID's own `Parse(string)` / `TryParse(string, out T)` static methods instead of constructing the ID from a value pre-parsed via the backing type. Catches `new XxxId(Guid.Parse(s))` and `if (Guid.TryParse(s, out var v)) { new XxxId(v); }` (and the predeclared-local form `Guid g; if (Guid.TryParse(s, out g)) { new XxxId(g); }`). Overload-matching covers any overload pair where the target ID exposes a sibling `Parse`/`TryParse` with the same parameter list (with `out backingType` swapped to `out idType` for `TryParse`). Structural fallback handles cross-project IDs whose `[StronglyTypedIdAttribute]` is stripped from metadata via `[Conditional]`. |
| NLF0014 | Usage    | Warning  | Call to `Type.Parse` / `Type.TryParse` should pass an explicit `IFormatProvider` when an overload accepting one exists. Stricter than `CA1305`: no per-type exclusions &mdash; `Guid.Parse(s)`, `DateTime.Parse(s)`, `int.Parse(s)`, `decimal.Parse(s)`, and `int.TryParse(s, out v)` all flag. Single-step upgrade only: overloads requiring additional non-`IFormatProvider` parameters do not trigger the diagnostic. Calls whose overload already includes any `IFormatProvider` (including `null` or subclasses like `CultureInfo`) are silent. |
| NLF0015 | Usage    | Warning  | A method whose name uses the `Try` prefix (e.g. `TryGetAsync`, `TryParse`) must return `TriedEx<T>`, `TriedNullEx<T>`, or `Exception?` (optionally wrapped in `Task<>`/`ValueTask<>`). The prefix is a contract: it tells callers the method swallows exceptions into a result they must inspect via `.Success` before reading `.Value`. Using it on a method that returns `bool`, `T?`, `void`, etc. silently breaks the convention every other Try method follows. Skips overrides, interface implementations, members of `NexusLabs.Framework.Try`, and underscore-delimited test method names (`TryAsync_Scenario_Expectation`). |
| NLF0016 | Usage    | Warning  | `HashSet<string>` constructors (every overload, including target-typed `new()`) and the `Enumerable.ToHashSet(IEnumerable<string>)` extension must pass `StringComparer.OrdinalIgnoreCase`. A string-keyed set without that explicit comparer treats `"Foo"` and `"foo"` as different keys &mdash; almost never the intent for config keys, header names, file paths, or identifiers. Deliberately rejects `StringComparer.Ordinal` (still case-sensitive), `CurrentCulture(IgnoreCase)` (locale-dependent), and `InvariantCulture(IgnoreCase)` (slower, unbounded culture lookup) &mdash; `OrdinalIgnoreCase` is the only choice that is both fast and locale-stable. Non-string `HashSet<T>` and `ImmutableHashSet<string>` are unaffected. |
| NLF0017 | Usage    | Warning  | Classes implementing `Carter.ICarterModule` must be declared `public sealed class`. Carter discovers modules via reflection over PUBLIC types only &mdash; a non-public module compiles cleanly but its routes silently return 404 at runtime with no build error. `sealed` is also required by convention because Carter modules are leaf classes; subclassing them creates duplicate route registrations. The analyzer matches `Carter.ICarterModule` by namespace + type-name in the consumer's compilation; **no Carter package reference is required in the analyzer assembly**. |
| NLF0018 | Usage    | Warning  | `CancellationToken` parameters must not carry a default value. Optional tokens let callers silently drop the token so downstream I/O ignores cancellation and never stops cleanly on shutdown, request-abort, or timeout. Applies to every method, constructor, local function, and delegate signature &mdash; no scope filter, no kind filter, no built-in escape hatch. Pass `CancellationToken.None` explicitly when truly no token is available, or suppress per-method with `#pragma warning disable NLF0018` for intentional public-API ergonomic defaults or `[EnumeratorCancellation]` iterators. |
| NLF0019 | Performance | Warning | Allocating an empty collection &mdash; of *any* constructed type (`List<T>`, `Dictionary<K,V>`, `HashSet<T>`, `SortedDictionary`, `ObservableCollection`, user-defined collections, …) &mdash; only to expose it through a read-only collection interface (`IEnumerable<T>`, `IReadOnlyCollection<T>`, `IReadOnlyList<T>`, `IReadOnlyDictionary<TKey,TValue>`, `IReadOnlySet<T>`) wastes a per-call heap allocation the caller can never mutate. Return a shared empty instead: `[]` (which the compiler lowers to the cached `Array.Empty<T>()` singleton) for the list family, `ReadOnlyDictionary<TKey,TValue>.Empty`, or `FrozenSet<T>.Empty`. Keys on the **converted** type, so `var x = new List<int>(); Populate(x); return x;` is left alone (the creation's converted type is the mutable `List<int>`, not the interface). The mutable interfaces (`IList`/`ICollection`/`ISet`/`IDictionary`) are excluded because their callers may `Add`, and zero-length arrays (`new T[0]`) are left to the built-in `CA1825`. The dictionary/set branches self-gate on the `.Empty` member existing (.NET 8+). Ships with a code fix; the package's first `Performance`-category rule. |

Internal &mdash; test infrastructure:

- `AnalyzerVerifier<T>.VerifyAnalyzerWithAdditionalProjectAsync` added (NLF0013) to support tests that need a separate referenced project, used by the NLF0013 cross-project structural-fallback test where the `[StronglyTypedIdAttribute]` is `[Conditional]`-stripped from the referenced metadata.

### NexusLabs.Xunit.Assertions

Internal &mdash; NLF0015 acknowledgements:

- Four `TriedEx` / `TriedNullEx`-aware assertion helpers on `AssertAugmentations` &mdash; `TrySucceeded<T>(TriedEx<T>, string)`, `TrySucceeded<T>(TriedNullEx<T?>, string)`, `TryFailed<T, TException>(TriedEx<T>, string)`, and `TryFailed<T, TException>(TriedNullEx<T?>, string)` &mdash; keep their `Try`-prefixed names but are now wrapped in per-method `#pragma warning disable NLF0015` / `restore` blocks with an inline comment naming the rationale: the prefix here refers to the asserted-on `Tried*` type, not to a Try-result return contract. No behavioural change.

### NexusLabs.Data.Sql

Internal &mdash; follow-through for the `IDbConnectionFactory` signature change:

- `PredicateAsyncDbConnectionFactory.CreateNewConnectionAsync(CancellationToken cancellationToken)` &mdash; `= default` removed to match the interface (the rule fires on both interface and impl independently). No behavioural change.
- `PredicateAsyncDbConnectionFactory.OpenNewConnectionAsync(CancellationToken cancellationToken)` &mdash; same as above. No behavioural change.

### NexusLabs.Data.Sql.MySql

Internal &mdash; follow-through for the `IDbConnectionFactory` / `IAsyncDbDataReader` signature changes:

- `MySqlConnectionFactory.CreateNewConnectionAsync(CancellationToken cancellationToken)` &mdash; `= default` removed to match the interface. No behavioural change.
- `MySqlConnectionFactory.OpenNewConnectionAsync(CancellationToken cancellationToken)` &mdash; same as above. No behavioural change.
- `AsyncMySqlDataReader.ReadAsync(CancellationToken cancellationToken)` &mdash; `= default` removed to match `IAsyncDbDataReader.ReadAsync`. No behavioural change.

### NexusLabs.CodeAnalysis.Testing.TUnit

Lockstep version bump only &mdash; no behavioural changes since `0.2.2`.

---

## [0.2.2] &mdash; 2026-05-29

Analyzer-heavy release. `NexusLabs.Framework.Analyzers` grows from one rule to
twelve diagnostics plus a diagnostic suppressor, ships its first code fix, and
splits its `CodeFixProvider` into a sibling assembly under one `.nupkg` to
comply with Roslyn rule `RS1038`. `NexusLabs.Framework` adds
`[TransfersOwnership]` (the attribute the suppressor reads) and implements
`IDisposable` / `IAsyncDisposable` on the `Tried*` family so callers can wrap
disposable results with `using` without first checking `Success`.
`NexusLabs.Data.Sql` ships a connection-pool resilience pass (timeout-bounded
lease acquisition + a typed exhausted-pool exception &mdash; **BREAKING**),
ported from a downstream MySQL outage post-mortem. The solution now dogfoods
its own analyzers end-to-end.

### NexusLabs.Framework

Added &mdash; `IDisposable` / `IAsyncDisposable` on `Tried*`:

- `Tried<T>`, `TriedEx<T>`, and `TriedNullEx<T?>` now implement `IDisposable` and `IAsyncDisposable`. The `Dispose` body is `if (Success && _value is IDisposable d) d.Dispose();`, so the wrapped value is disposed only on a Success result and only when `T` actually implements `IDisposable`. For non-disposable `T` the type check is JIT-specialized to a constant and the method inlines to a no-op &mdash; opting in costs nothing at runtime. `DisposeAsync` prefers `IAsyncDisposable`, falls back to `IDisposable`, and otherwise returns a completed task. `Error` is never disposed.
- Both interfaces are implemented directly (not via duck-typed `Dispose` methods) because pattern-based `using` on non-`ref` structs emits `CS1674`. The structs cannot be `ref struct` because they flow through `Task<T>` in async state machines.
- Enables the idiom `using var result = TryDoThing();` so disposal is guaranteed on every exit path without first guarding on `Success`. Paired with new analyzer rule **NLF0011** that flags `Tried*<T>` locals (where `T` is disposable) that are dropped on the floor.

Added &mdash; `TransfersOwnershipAttribute`:

- `NexusLabs.Framework.TransfersOwnershipAttribute` &mdash; new attribute that documents intentional disposal-ownership transfer to a declaring type. Two shapes:
  - **Shape B (direct, parameterless)** &mdash; applied to a disposable field or property, authorises `Dispose` calls on it.
  - **Shape A (conditional, strict targets)** &mdash; applied to a `bool` field, property, or parameter that lists one or more target member names (use `nameof(...)`), authorises disposal of the listed members inside an `if` whose condition reads the annotated flag.
- Consumed by the new `TransfersOwnershipDisposeSuppressor` (NLFSUP001) shipped in `NexusLabs.Framework.Analyzers` &mdash; the attribute is the sole public surface the suppressor reads.
- `AttributeUsage` is `Field | Property | Parameter`, `AllowMultiple = false`, `Inherited = false`.

Added &mdash; bounded `SemaphoreSlim` extensions on `SemaphoreSlimExtensions`:

- `SemaphoreSlim.AcquireAsync(TimeSpan timeout, CancellationToken cancellationToken)` &mdash; waits up to `timeout` for a slot, throws `TimeoutException` if the budget elapses with no slot acquired. `cancellationToken` is required (no default) so source-level disambiguation against the existing `AcquireAsync(CancellationToken)` overload is unambiguous.
- `SemaphoreSlim.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken)` &mdash; same wait semantics but returns `Task<AsyncSemaphoreLease?>` &mdash; `null` on timeout instead of throwing. Prefer this when a `null` branch is cheaper than catching `TimeoutException`.
- Both overloads validate `timeout` (must be non-negative or `Timeout.InfiniteTimeSpan`), short-circuit `TimeSpan.Zero` to a non-blocking acquire (via underlying `SemaphoreSlim.WaitAsync(TimeSpan, CancellationToken)`), and propagate `OperationCanceledException` on cancellation without consuming a slot.

### NexusLabs.Framework.Analyzers

The analyzer package grows from one rule (NLF0001) to twelve diagnostics plus
a diagnostic suppressor (NLFSUP001), ships its first code fix (for NLF0010),
and splits the `CodeFixProvider` into a sibling assembly under one `.nupkg`
to comply with Roslyn rule `RS1038`. The package retains the `netstandard2.0`
target, the `<DevelopmentDependency>true</DevelopmentDependency>` shape, and
the `NLF` diagnostic-ID prefix. Opt out of any individual rule per project
via `dotnet_diagnostic.NLFxxxx.severity = none` in `.editorconfig`.

Added &mdash; analyzer rules:

| ID      | Category | Severity | Summary |
|---------|----------|----------|---------|
| NLF0002 | Usage    | Warning  | Check `result.Success` before accessing `Value`. Guards on the success branch are recognised (`if (result.Success) ...`, ternaries, short-circuit `&&`, early `return`/`throw`/`break`/`continue`). |
| NLF0003 | Usage    | Warning  | Check `result.Success` is false before accessing `Error`. Same control-flow recognition as NLF0002. |
| NLF0004 | Usage    | Warning  | Once `Success` has been verified false, `Error` is guaranteed non-null &mdash; drop redundant `result.Error == null` / `is null` / `!= null` checks. |
| NLF0005 | Usage    | Warning  | An `Exception` returned from a Success-false branch must reference the original `result.Error` (direct return, `new MyException("...", result.Error)`, or aggregated via `new AggregateException(result.Error, ...)`). Returning a fresh exception with no reference silently drops the failure. |
| NLF0006 | Usage    | Warning  | An async method whose body is exactly one whole-body `try`/`catch` should use `Try.Async`, `Try.GetAsync`, or `Try.GetOrNullAsync` to centralise the catch policy. |
| NLF0007 | Usage    | Warning  | When a whole-body `Try.*` wrapper is used, pass an `ILogger` so caught exceptions are logged. The logger-less overloads are for nested or transient usage where the caller already owns the logging context. |
| NLF0008 | Usage    | Warning  | Don't `throw` inside a `Try.*` callback &mdash; `throw` outside the callback or `return new TriedEx<T>(ex)` from the lambda so the helper captures the failure as `Error`. |
| NLF0009 | Usage    | Warning  | An async method whose return type is `Task<TriedEx<T>>` or `Task<TriedNullEx<T>>` should wrap its body with `Try.GetAsync` / `Try.GetOrNullAsync` &mdash; otherwise an uncaught exception faults the `Task` instead of populating `Error`. Direct pass-through (`=> await OtherTryMethod()`) is allowed. |
| NLF0010 | Usage    | Warning  | The opening `"""` of a multi-line raw string literal must be on its own line, aligned with the closing `"""`. Single-line raw strings (`var s = """value""";`) are exempt. **Ships with a code fix** that moves the opening token to its own line at the correct indent. |
| NLF0011 | Usage    | Warning  | A local of type `Tried<T>` / `TriedEx<T>` / `TriedNullEx<T?>` whose `T` implements `IDisposable` (or `IAsyncDisposable`) and is dropped on the floor &mdash; not consumed by `using`, not returned, not passed to another method, no explicit `Dispose` &mdash; leaks the wrapped value. Prefer `using var local = TryDoThing();`. Passing the local to another method is treated as ownership transfer. |
| NLF0012 | Usage    | Warning  | Parameterless `[TransfersOwnership]` on a member whose type is not `IDisposable` / `IAsyncDisposable` is silently inert (NLFSUP001 will never act on it). Add target names for Shape A (e.g. `[TransfersOwnership(nameof(_field))]`), or move the attribute onto the disposable member itself for Shape B. |

Added &mdash; diagnostic suppressor:

- **NLFSUP001** &mdash; `TransfersOwnershipDisposeSuppressor` suppresses `IDisposableAnalyzers` rule **IDISP007 ("Don't dispose injected")** when the disposal target &mdash; or a `bool` flag guarding the dispose call &mdash; is annotated with `[NexusLabs.Framework.TransfersOwnership]`. Recognised shapes: (1) field/property carrying parameterless `[TransfersOwnership]` disposed directly; (2) dispose call inside `if (<bool>)` where the boolean member carries `[TransfersOwnership(nameof(<field>))]` and the dispose receiver matches one of the listed targets. Awaited dispose calls (`await x.DisposeAsync()`) are covered. Disjunctions (`||`) in the guard condition and empty target lists are deliberately **not** honoured &mdash; silencing every dispose inside a guard regardless of which field it disposes is the bug class strict targeting prevents.

Changed:

- **`NexusLabs.Framework.Analyzers` now ships as two assemblies in one `.nupkg`**: `NexusLabs.Framework.Analyzers.dll` (the `DiagnosticAnalyzer`s and the `DiagnosticSuppressor`) and `NexusLabs.Framework.Analyzers.CodeFixes.dll` (the `CodeFixProvider`s). The split is required by `RS1038` &mdash; analyzer assemblies cannot reference `Microsoft.CodeAnalysis.Workspaces`, which `CodeFixProvider` needs. Consumers add a single `<PackageReference Include="NexusLabs.Framework.Analyzers" ... />` and get both DLLs; nothing changes at the call site.
- Diagnostic messages were rewritten to be **LLM-actionable**: every `messageFormat` now lists the concrete remediation (e.g. *"Guard with `if (result.Success)` first, or use `result.Match(onSuccess, onError)` to handle both branches in a single expression"*). Every rule has `helpLinkUri` pointing at `docs/analyzers/NLFxxxx.md`.

Internal:

- Adopted `IDisposableAnalyzers 4.0.8` across `src/`. The suppressor plus new `[TransfersOwnership]` annotations replaced 12 `[SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP007")]` annotations across the solution.
- NLF analyzer wiring is centralised in `Directory.Build.props`. All `src/` projects except the two analyzer assemblies reference `NexusLabs.Framework.Analyzers` and `IDisposableAnalyzers` &mdash; the solution dogfoods its own rules end-to-end.

### NexusLabs.Xunit.Assertions

Lockstep version bump only &mdash; no behavioural changes since `0.2.1`.

### NexusLabs.CodeAnalysis.Testing.TUnit

Lockstep version bump only &mdash; no behavioural changes since `0.2.1`.

### NexusLabs.Data.Sql

Connection-pool resilience pass: timeout-bounded lease acquisition and a typed
exhausted-pool exception. Ported from a downstream MySQL outage post-mortem.

Added:

- `ConnectionPoolExhaustedException` &mdash; new sealed exception type. Derives from `InvalidOperationException` so existing connection-management catch handlers stay compatible. Carries an `AcquisitionTimeout` property so operators can correlate logs with the configured cap.

Changed (**BREAKING**):

- `LeasedAsyncDbConnection` ctor signature is now `(IAsyncDbConnection inner, SemaphoreSlim leaseSemaphore, TimeSpan acquisitionTimeout)`. The 2-arg ctor is removed entirely. Callers must specify an explicit acquisition budget; pass `Timeout.InfiniteTimeSpan` to keep the previous wait-until-the-caller-cancels behaviour (not recommended in production paths &mdash; saturated pool + never-cancelled token previously hung forever).
- `AsyncDbDecoratorExtensions.WithLease(this IAsyncDbConnection, SemaphoreSlim)` &rarr; `WithLease(this IAsyncDbConnection, SemaphoreSlim, TimeSpan acquisitionTimeout)`. Same migration story as the ctor.
- `OpenAsync` on `LeasedAsyncDbConnection` called twice without an intervening `Close` / `DisposeAsync` now throws `InvalidOperationException` immediately (matching `System.Data` conventions) instead of acquiring a second slot and releasing the prior lease. Removes a non-obvious magic behaviour that would deadlock at pool-size-1.
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

Tested:

- Four deterministic race-coverage tests for `LeasedAsyncDbConnection` (TCS + counting `SemaphoreSlim`; no arbitrary timings or `Task.Delay`): concurrent-Open-past-the-early-guard, Close-racing-with-Open-in-flight, concurrent-Opens-all-failing-in-inner, and mixed-success-and-failure under contention. Each test was mutation-verified &mdash; removing the corresponding production safeguard fails the test deterministically.

Internal &mdash; replaced 12 `[SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP007")]` annotations across the solution with the new `[TransfersOwnership]` attribute (suppressed via the bundled `TransfersOwnershipDisposeSuppressor`).

### NexusLabs.Data.Sql.MySql

Lockstep version bump only &mdash; adapter code (`AsyncMySqlConnection`,
`AsyncMySqlCommand`, `AsyncMySqlDataReader`) is unchanged behaviourally since
`0.2.1`. The only edits are `[TransfersOwnership]` annotations applied during
the IDISP007 cleanup pass; no public API change.

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
