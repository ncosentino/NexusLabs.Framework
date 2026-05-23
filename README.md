# NexusLabs.Framework

Cross-cutting C# utilities used across Nexus Software Labs projects: a result-pattern type system (`Tried`/`TriedEx`/`TriedNullEx`), a `Safely` try/catch facade, stream wrappers (`StreamWithLength`, `ReadOnlySubstream`, `StreamPump`), async-aware event-handler extensions, process diagnostics helpers, and async ADO.NET interface shapes.

## Install

```
dotnet add package NexusLabs.Framework
```

Targets `net10.0`. For earlier .NET versions, pin to the 0.x line.

## What's in the package

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
