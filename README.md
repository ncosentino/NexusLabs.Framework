# NexusLabs.Framework (and friends)

A multi-package repository for cross-cutting NexusLabs C# tooling. Currently ships:

| Package | Purpose |
|---|---|
| [`NexusLabs.Framework`](https://www.nuget.org/packages/NexusLabs.Framework) | Runtime utilities: result pattern (`Tried`/`TriedEx`/`TriedNullEx`), `Try` orchestration, stream wrappers, `AsyncSemaphoreLease` concurrency primitive, `ArrayPool` renting handles (`RentedSpan`/`RentedMemory`), async event-handler helpers, async ADO.NET interface shapes, process diagnostics. |
| [`NexusLabs.Framework.Analyzers`](https://www.nuget.org/packages/NexusLabs.Framework.Analyzers) | Roslyn analyzers for codebase hygiene and correct use of `NexusLabs.Framework` types. Test-specific and data-layer analyzers ship in separate packages. |
| [`NexusLabs.StronglyTypedIds`](https://www.nuget.org/packages/NexusLabs.StronglyTypedIds) | Additive UUIDv7 creation for GUID-backed strongly typed identifiers. Generates `Create()` methods, supplies TimeProvider-aware factories and DI registration, and bundles analyzers that reject UUIDv4-producing creation paths. |
| [`NexusLabs.Xunit.Assertions`](https://www.nuget.org/packages/NexusLabs.Xunit.Assertions) | xUnit.v3 assertion helpers that integrate with the Framework result-pattern types and HTTP response shapes. Uses C# 14 `extension(Assert)` blocks. |
| [`NexusLabs.TUnit.Assertions`](https://www.nuget.org/packages/NexusLabs.TUnit.Assertions) | TUnit-native assertions for `TriedEx` and `TriedNullEx`. `Succeeded()` and `Failed()` validate the complete result and return the successful value or captured exception from `await`. Includes the NLT0001 usage analyzer. |
| [`NexusLabs.CodeAnalysis.Testing.TUnit`](https://www.nuget.org/packages/NexusLabs.CodeAnalysis.Testing.TUnit) | TUnit-flavored `IVerifier` for `Microsoft.CodeAnalysis.Testing`. Lets TUnit-based test projects use the full `CSharpAnalyzerTest<TAnalyzer, TVerifier>` harness, which Microsoft ships verifiers for in xUnit/NUnit/MSTest but not TUnit. |
| [`NexusLabs.Testing.Time`](https://www.nuget.org/packages/NexusLabs.Testing.Time) | Test-time controls for `System.TimeProvider`. `RegistrationObservingTimeProvider` reports when the code under test arms a timer so a test advances the clock only after the registration it depends on; `AdvanceUntilAsync` is a bounded pump for when the expected registration count is unknown. |
| [`NexusLabs.Data.Sql`](https://www.nuget.org/packages/NexusLabs.Data.Sql) | Provider-agnostic decorators around `IAsyncDbConnection`/`IAsyncDbCommand`: bounded connection-lease (built on `AsyncSemaphoreLease`), open-tracking diagnostics, `ILogger` command logging, predicate-built factory. |
| [`NexusLabs.Data.Sql.MySql`](https://www.nuget.org/packages/NexusLabs.Data.Sql.MySql) | MySQL provider for the `NexusLabs.Data.Sql` surface and `IAsyncDb*` interfaces. Builds connection strings safely via `MySqlConnectionStringBuilder`. |

## Install

```
dotnet add package NexusLabs.Framework
dotnet add package NexusLabs.Framework.Analyzers # opt-in lint rules
dotnet add package NexusLabs.StronglyTypedIds    # UUIDv7 creation + bundled analyzers
dotnet add package NexusLabs.Xunit.Assertions    # only in test projects
dotnet add package NexusLabs.TUnit.Assertions    # TUnit assertions + bundled analyzer
dotnet add package NexusLabs.CodeAnalysis.Testing.TUnit  # for TUnit-based analyzer test projects
dotnet add package NexusLabs.Testing.Time        # only in test projects
dotnet add package NexusLabs.Data.Sql            # provider-agnostic decorators
dotnet add package NexusLabs.Data.Sql.MySql      # adds MySql.Data backed factory
```

## Pull request delivery

Repository changes are delivered through protected pull requests under the
Genesis PR-first model. The machine-readable delivery contract is
`.github/genesis-delivery.json`; contributor and agent guidance is in
`AGENTS.md`.

Runtime packages target `net10.0`; Roslyn analyzer assemblies target
`netstandard2.0`. For earlier .NET versions, pin to a 0.1.x of
`NexusLabs.Framework`.

## What's in `NexusLabs.Framework`

Runtime utilities for cross-cutting C# concerns: a result-pattern type family
(`Tried`/`TriedEx`/`TriedNullEx`) with `Safely` / `Try` orchestration helpers,
stream wrappers, `AsyncSemaphoreLease` and related concurrency primitives,
`ArrayPool` renting handles (`RentedSpan`/`RentedMemory` + `RentSpan`/`RentMemory`),
async event-handler glue, async ADO.NET interface shapes, and process
diagnostics. The deprecated `ITimeProvider` ships for one more 0.x release;
migrate to BCL `System.TimeProvider`.

The authoritative list of public types is the source tree under
`src/NexusLabs.Framework/` and the XML doc comments shipped in the package.
See [CHANGELOG.md](CHANGELOG.md) for what landed in each version.

## UUIDv7 strongly typed identifiers

`NexusLabs.StronglyTypedIds` composes the built-in GUID template with a small
additive template. Existing parsing, formatting, equality, JSON conversion, and
type conversion remain generated by the underlying identifier package:

```csharp
using NexusLabs.StronglyTypedIds;
using StronglyTypedIds;

[StronglyTypedId(Template.Guid, GuidIdTemplates.UuidV7)]
public readonly partial struct OrderId;

OrderId orderId = OrderId.Create();
```

Pass a `TimeProvider` when the timestamp must be controlled:

```csharp
OrderId orderId = OrderId.Create(timeProvider);
```

Or register the generic, mockable generation service:

```csharp
services.AddUuidV7IdentifierGeneration();

IUuidV7IdentifierGenerator<OrderId> generator =
    serviceProvider.GetRequiredService<IUuidV7IdentifierGenerator<OrderId>>();
OrderId orderId = generator.Create();
```

The template provides a UUIDv7 **creation policy**, not a value invariant. The
GUID constructor, parsing, deserialization, `default`, and `Empty` can preserve
arbitrary GUID values for rehydration. The IntelliSense documentation on
`GuidIdTemplates.UuidV7` calls out this boundary.

The package bundles two error-level analyzers:

- **NLS0001** replaces the built-in `OrderId.New()` UUIDv4 path with
  `OrderId.Create()`.
- **NLS0002** replaces `new OrderId(Guid.NewGuid())` while allowing construction
  from externally sourced GUID values.

UUIDv7 values are timestamp ordered across milliseconds; the remaining bits are
random, so the package does not claim strict within-millisecond ordering or
database-independent index ordering.

## Result pattern

```csharp
TriedEx<int> result = Safely.GetResultOrException(() => int.Parse(input));

result.Match(
    onSuccess: value => Console.WriteLine($"parsed: {value}"),
    onError: ex => Console.WriteLine($"failed: {ex.Message}"));
```

## TUnit result assertions

`NexusLabs.TUnit.Assertions` integrates the result pattern with TUnit's native
fluent assertions:

```csharp
using NexusLabs.Framework;
using NexusLabs.TUnit.Assertions;

TriedEx<ThingId> result =
    await service.TryCreateAsync(input, userId, cancellationToken);

var thingId = await Assert.That(result)
    .Succeeded()
    .Because("The service should create the thing");
```

Failure assertions return the original exception and can require an assignable
exception type:

```csharp
var error = await Assert.That(result)
    .Failed()
    .With<ArgumentException>()
    .Because("Invalid input should be rejected");
```

The package includes **NLT0001**, which reports direct assertions such as
`Assert.That(result.Success)` and points callers to the result-level
`Succeeded()` / `Failed()` API.

## Controlling time in tests

`FakeTimeProvider.Advance` only fires timers that are **already registered**.
When the code under test arms its delay on a task the test cannot observe, a
single advance can land first, leaving the timer due at a simulated time the
test never reaches — the awaited work then hangs until the harness gives up.

`NexusLabs.Testing.Time` removes that race rather than racing it faster. Wait
for the registration, then advance exactly once:

```csharp
using NexusLabs.Testing.Time;

var clock = new RegistrationObservingTimeProvider(); // is-a FakeTimeProvider
var runTask = runner.RunAsync(definition, cancellationToken);

await clock.WaitForArmedTimersAsync(1, TimeSpan.FromSeconds(5), cancellationToken);
clock.Advance(TimeSpan.FromMinutes(1));

var result = await runTask;
```

`Task.Delay(delay, clock)`, `new CancellationTokenSource(delay, clock)` and
`new PeriodicTimer(period, clock)` all arm through a single `CreateTimer` call,
so one observation covers each of them.

When the number of expected registrations is not knowable, fall back to the
pump. It is bounded on both real and simulated time, and reports rather than
throws:

```csharp
var outcome = await clock.AdvanceUntilAsync(
    () => Volatile.Read(ref retryAttempts) == 2,
    increment: TimeSpan.FromMinutes(1),
    timeout: TimeSpan.FromSeconds(10),
    cancellationToken,
    maxSimulatedAdvance: TimeSpan.FromHours(1));

Assert.True(outcome.Succeeded, outcome.Describe());
```

The pump converges rather than synchronising, so it can still lose the race on
a loaded machine — prefer `WaitForArmedTimersAsync` whenever the count is known.

Four analyzers in `NexusLabs.Framework.Analyzers` cover the surrounding
mistakes:

| Rule | Severity | Reports |
| --- | --- | --- |
| `NLF0025` | Warning | A call or construction with a `TimeProvider` overload, made where a clock is already reachable but was not passed. |
| `NLF0026` | Info | The same, where no clock is in scope. A design observation rather than a defect. |
| `NLF0027` | Warning | An interface that reimplements `TimeProvider`, including the common clock-plus-delay shape. |
| `NLF0028` | Warning | A `FakeTimeProvider` subclass whose `CreateTimer` override never calls `base.CreateTimer`, silently disabling virtual time. |

## Archived packages

Six packages from this repository were archived as part of 0.2.0:

- `NexusLabs.Autofac`, `NexusLabs.Collections.Generic`, `NexusLabs.Contracts`, `NexusLabs.Dynamo`, `NexusLabs.Reflection`, `NexusLabs.Testing.Xunit`

The 0.x lines remain on nuget.org. Source is preserved on the `release/0.x` branch. See [docs/archived-packages/](docs/archived-packages/README.md) for per-package migration guidance.

## License

[MIT](LICENSE) © Nexus Software Labs
