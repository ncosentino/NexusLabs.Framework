---
applyTo: "**/*.cs"
---

# Time Provider Rules

## NEVER use DateTime.Now, DateTime.UtcNow, DateTimeOffset.Now, or DateTimeOffset.UtcNow

Direct access to system time is **FORBIDDEN** in production code. It makes code untestable because time cannot be controlled in tests.

This also applies to anything that internally reads system time without taking a `TimeProvider`. Notable examples:

- `Stopwatch.StartNew()` / `Stopwatch.GetTimestamp()` — use `_timeProvider.GetTimestamp()` and `_timeProvider.GetElapsedTime(startTimestamp)` instead.
- `Task.Delay(delay, cancellationToken)` — use the overload that accepts a `TimeProvider`: `Task.Delay(delay, _timeProvider, cancellationToken)`.
- `new Timer(...)` / `new PeriodicTimer(delay)` — use `_timeProvider.CreateTimer(...)` or `new PeriodicTimer(delay, _timeProvider)`.

## Use TimeProvider instead

Inject `TimeProvider` — the abstract class from the `System` namespace, built into .NET 8+. There is no `ITimeProvider` interface; the type to depend on is `TimeProvider` itself.

```csharp
internal sealed class MyUnitOfWork(
    ILogger<MyUnitOfWork> _logger,
    TimeProvider _timeProvider,
    MyRepository _repository) : IMyUnitOfWork
{
    public async Task<TriedEx<Thing>> TryCreateAsync(
        CreateThingInput input,
        string ownerUserId,
        CancellationToken cancellationToken) => await
    Try.GetAsync<Thing>(_logger, async () =>
    {
        var now = _timeProvider.GetUtcNow().DateTime;
        // ...
    });
}
```

## Return types

`GetUtcNow()` returns `DateTimeOffset`. Use:
- `_timeProvider.GetUtcNow()` when you need `DateTimeOffset`
- `_timeProvider.GetUtcNow().DateTime` when you need `DateTime`

## Registration

`TimeProvider.System` is a static singleton instance, not an auto-discoverable concrete class, so Needlr will not register it for you. Register it manually inside an `IServiceCollectionPlugin.Configure()` in whichever Kernel or App project owns framework-infrastructure registrations for the template:

```csharp
internal sealed class TimePlugin : IServiceCollectionPlugin
{
    public void Configure(ServiceCollectionPluginOptions options)
    {
        options.Services.AddSingleton(TimeProvider.System);
    }
}
```

Do NOT register it from `Program.cs` directly — Needlr's plugin discovery is the canonical place for "things Needlr cannot auto-discover", and keeping it there means tests that build their service provider through the same plugin pipeline pick it up automatically.

## Test code

Test code is exempt from the "no `DateTime.UtcNow`" rule because tests need to control time deterministically. More specific instructions apply inside test projects — those cover how to build and inject `FakeTimeProvider`, how to advance it, and how to override the production `TimeProvider` registration through the project's test fixture.
