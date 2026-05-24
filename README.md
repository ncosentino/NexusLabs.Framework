# NexusLabs.Framework (and friends)

A multi-package repository for cross-cutting NexusLabs C# tooling. Currently ships:

| Package | Purpose |
|---|---|
| [`NexusLabs.Framework`](https://www.nuget.org/packages/NexusLabs.Framework) | Runtime utilities: result pattern (`Tried`/`TriedEx`/`TriedNullEx`), `Try` orchestration, stream wrappers, `AsyncSemaphoreLease` concurrency primitive, async event-handler helpers, async ADO.NET interface shapes, process diagnostics. |
| [`NexusLabs.Framework.Analyzers`](https://www.nuget.org/packages/NexusLabs.Framework.Analyzers) | Roslyn analyzers for codebase hygiene and correct use of `NexusLabs.Framework` types. Test-specific and data-layer analyzers ship in separate packages. |
| [`NexusLabs.Xunit.Assertions`](https://www.nuget.org/packages/NexusLabs.Xunit.Assertions) | xUnit.v3 assertion helpers that integrate with the Framework result-pattern types and HTTP response shapes. Uses C# 14 `extension(Assert)` blocks. |
| [`NexusLabs.Data.Sql`](https://www.nuget.org/packages/NexusLabs.Data.Sql) | Provider-agnostic decorators around `IAsyncDbConnection`/`IAsyncDbCommand`: bounded connection-lease (built on `AsyncSemaphoreLease`), open-tracking diagnostics, `ILogger` command logging, predicate-built factory. |
| [`NexusLabs.Data.Sql.MySql`](https://www.nuget.org/packages/NexusLabs.Data.Sql.MySql) | MySQL provider for the `NexusLabs.Data.Sql` surface and `IAsyncDb*` interfaces. Builds connection strings safely via `MySqlConnectionStringBuilder`. |

## Install

```
dotnet add package NexusLabs.Framework
dotnet add package NexusLabs.Framework.Analyzers # opt-in lint rules
dotnet add package NexusLabs.Xunit.Assertions    # only in test projects
dotnet add package NexusLabs.Data.Sql            # provider-agnostic decorators
dotnet add package NexusLabs.Data.Sql.MySql      # adds MySql.Data backed factory
```

Both packages target `net10.0`. For earlier .NET versions, pin to a 0.1.x of `NexusLabs.Framework`.

## What's in `NexusLabs.Framework`

Runtime utilities for cross-cutting C# concerns: a result-pattern type family
(`Tried`/`TriedEx`/`TriedNullEx`) with `Safely` / `Try` orchestration helpers,
stream wrappers, `AsyncSemaphoreLease` and related concurrency primitives,
async event-handler glue, async ADO.NET interface shapes, and process
diagnostics. The deprecated `ITimeProvider` ships for one more 0.x release;
migrate to BCL `System.TimeProvider`.

The authoritative list of public types is the source tree under
`src/NexusLabs.Framework/` and the XML doc comments shipped in the package.
See [CHANGELOG.md](CHANGELOG.md) for what landed in each version.

## Result pattern

```csharp
TriedEx<int> result = Safely.GetResultOrException(() => int.Parse(input));

result.Match(
    onSuccess: value => Console.WriteLine($"parsed: {value}"),
    onError: ex => Console.WriteLine($"failed: {ex.Message}"));
```

## Archived packages

Six packages from this repository were archived as part of 0.2.0:

- `NexusLabs.Autofac`, `NexusLabs.Collections.Generic`, `NexusLabs.Contracts`, `NexusLabs.Dynamo`, `NexusLabs.Reflection`, `NexusLabs.Testing.Xunit`

The 0.x lines remain on nuget.org. Source is preserved on the `release/0.x` branch. See [docs/archived-packages/](docs/archived-packages/README.md) for per-package migration guidance.

## License

[MIT](LICENSE) © Nexus Software Labs
