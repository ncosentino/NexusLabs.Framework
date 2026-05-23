# NexusLabs.Framework (and friends)

A multi-package repository for cross-cutting NexusLabs C# tooling. Currently ships:

| Package | Purpose |
|---|---|
| [`NexusLabs.Framework`](https://www.nuget.org/packages/NexusLabs.Framework) | Runtime utilities: result pattern (`Tried`/`TriedEx`/`TriedNullEx`), stream wrappers, async event-handler helpers, async ADO.NET interface shapes, process diagnostics. |
| [`NexusLabs.Xunit.Assertions`](https://www.nuget.org/packages/NexusLabs.Xunit.Assertions) | xUnit.v3 assertion helpers that integrate with the Framework result-pattern types and HTTP response shapes. Uses C# 14 `extension(Assert)` blocks. |

## Install

```
dotnet add package NexusLabs.Framework
dotnet add package NexusLabs.Xunit.Assertions  # only in test projects
```

Both packages target `net10.0`. For earlier .NET versions, pin to a 0.1.x of `NexusLabs.Framework`.

## What's in `NexusLabs.Framework`

| Concern | Types |
|---|---|
| Result pattern | `Tried<T>`, `TriedEx<T>`, `TriedNullEx<T>`, `Safely`, `ExceptionHelper` |
| IO | `StreamWithLength`, `ReadOnlySubstream`, `SubstreamOptions`, `StreamPump` |
| Diagnostics | `ProcessExtensions` (`WaitForExitAsync` with `beforeWaitCallback`) |
| Async events | `MulticastDelegateExtensions`, `EventExtensions`, `GenericEventExtensions`, `ActionExtensions`, `AsyncVoidHelper` |
| Async ADO.NET | `IAsyncDbConnection`, `IAsyncDbCommand`, `IAsyncDbDataReader`, `IDbConnectionFactory` |
| Tasks | `TaskExtensions` (`.Forget()`, `ToOrderedAsyncEnumerable`, `ToUnorderedAsyncEnumerable`) |
| Time (deprecated) | `ITimeProvider`, `TimeProviderWrapper` — use BCL `System.TimeProvider` instead |

See the [0.2.0 release notes](CHANGELOG.md) for migration details from prior 0.1.x.

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
