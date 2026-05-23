---
applyTo: "**/*.Tests/**/*.cs,**/*.Tests/*.cs"
---

# Time Provider Testing Rules

Test code is exempt from the production "no `DateTime.UtcNow`" rule because tests must control time deterministically. The general test rules forbid `Task.Delay` and `Thread.Sleep` — the positive prescription is **always use `FakeTimeProvider` and advance it explicitly**.

## Package and namespace

`FakeTimeProvider` is **not** in the BCL. It ships separately:

- **NuGet package:** `Microsoft.Extensions.TimeProvider.Testing`
- **Namespace:** `Microsoft.Extensions.Time.Testing`

The package name and the namespace deliberately differ; both are needed.

```xml
<PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" />
```

```csharp
using Microsoft.Extensions.Time.Testing;
```

Reference the package only from `*.Tests` projects.

## Construction and seeding

Always seed `FakeTimeProvider` to an explicit, fixed instant — never let it default to "now", because that re-introduces wall-clock nondeterminism into tests:

```csharp
var fakeTime = new FakeTimeProvider(
    startDateTime: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));

// Reset to a known instant mid-test if needed:
fakeTime.SetUtcNow(new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero));
```

## Advancing time deterministically

`Advance(TimeSpan)` synchronously moves the fake clock forward AND fires any timers / `Task.Delay` continuations scheduled at or before the new instant. This is the whole point of using `FakeTimeProvider` over real waits:

```csharp
fakeTime.Advance(TimeSpan.FromMinutes(5));
```

For tests where the SUT polls time in a tight loop, set `AutoAdvanceAmount` so every call to `GetUtcNow()` / `GetTimestamp()` ticks forward automatically:

```csharp
fakeTime.AutoAdvanceAmount = TimeSpan.FromMilliseconds(50);
```

## Cooperating APIs in the SUT

`FakeTimeProvider.Advance` only affects code paths that were written against `TimeProvider`. That means the SUT must use:

- `_timeProvider.CreateTimer(...)` instead of `new Timer(...)` / `new PeriodicTimer(delay)`.
- `Task.Delay(delay, _timeProvider, cancellationToken)` instead of `Task.Delay(delay, cancellationToken)`.
- `_timeProvider.GetTimestamp()` + `_timeProvider.GetElapsedTime(start)` instead of `Stopwatch`.

If a test would have reached for `Task.Delay` to "wait for the SUT to do its thing", that signal means the SUT itself was missing a `TimeProvider` argument upstream — fix the SUT, then advance the fake.

## Needlr override pattern — service tests

Do NOT `new` up `FakeTimeProvider` and pass it straight into the SUT's constructor when the rest of the dependency graph comes from DI. That bypasses the production registration graph and defeats the purpose of `TestFixtureBuilder`. Instead, override the registration through the project's standard fixture builder so the fake replaces `TimeProvider` everywhere it is injected:

```csharp
private static FakeTimeProvider? _fakeTime;
private static MyService? _sut;

[Before(Test)]
public void SetUp()
{
    _fakeTime ??= new FakeTimeProvider(
        startDateTime: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));

    _testFixture ??= new TestFixtureBuilder()
        .UsingMySqlContainerFixture(_mySqlFixture)
        .UsingDependency((TimeProvider)_fakeTime)
        .Build();

    _sut ??= _testFixture.GetRequiredService<MyService>();
}
```

The `(TimeProvider)` upcast is required: `UsingDependency` keys the registration off the argument's static type, so passing `_fakeTime` directly would register against `FakeTimeProvider`, and the SUT's `TimeProvider` constructor parameter would still resolve to the production `TimeProvider.System` registration. If your project's `TestFixtureBuilder` exposes a generic `UsingDependency<T>(T instance)` overload, `UsingDependency<TimeProvider>(_fakeTime)` is equivalent.

## Needlr override pattern — API tests

For API tests inheriting `ApiTestBase`, override inside `OnModifyBuilder` so the WebApplication built by the fixture sees the fake before any HTTP request fires:

```csharp
private FakeTimeProvider? _fakeTime;

protected override TestFixtureBuilder OnModifyBuilder(TestFixtureBuilder builder)
{
    _fakeTime ??= new FakeTimeProvider(
        startDateTime: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));

    return builder.UsingDependency((TimeProvider)_fakeTime);
}
```

Expose `_fakeTime` to individual tests via a protected property if they need to call `Advance`.
