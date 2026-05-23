---
applyTo: "**/*.Tests/**/*.cs,**/*.Tests/*.cs"
---

# General Test Rules

These rules apply to **all** test files. Some tests will also match other instruction files that have more specific instructions that will take precedent over these.

## Test framework

These rules apply to test code targeting either **TUnit** or **xUnit**. Structural rules (mock boundaries, strong assertions, no `#region` directives, no Arrange/Act/Assert comments, async/cancellation patterns, code-coverage prohibition) are framework-agnostic and apply identically.

Where attribute, fixture, or assertion syntax differs between frameworks, examples below show both. Pick the syntax matching the framework chosen for your project. **Genesis-scaffolded projects ship with TUnit by default** (selected via `global.json`'s `"runner": "Microsoft.Testing.Platform"` opt-in). Existing xUnit codebases continue to follow the same rules using xUnit syntax.

Quick framework-difference cheat sheet:

| Concept | TUnit | xUnit |
|---|---|---|
| Test attribute | `[Test]` | `[Fact]` |
| Parameterised test | `[Test]` + `[Arguments(...)]` (or `[MethodDataSource]`, `[ClassDataSource]`) | `[Theory]` + `[InlineData(...)]` (or `[MemberData]`, `[ClassData]`) |
| Assertion style | `await Assert.That(value).IsEqualTo(expected)` | `Assert.Equal(expected, value)` |
| Cancellation token | Inject `CancellationToken cancellationToken` parameter | `TestContext.Current.CancellationToken` (xUnit 3.x) |
| Class fixture | `[ClassDataSource<TFixture>(Shared = SharedType.PerClass)]` + ctor injection | `IClassFixture<TFixture>` interface + ctor injection |
| Per-test setup hook | `[Before(Test)]` method | Constructor (xUnit creates a new instance per test) |
| Per-class setup hook | `[Before(Class)] static` method | `IAsyncLifetime` on the class fixture |

## NEVER use `#region` or comment groups

- `#region` directives are FORBIDDEN in all test files.
- Comment separators are FORBIDDEN in all test files.
- Inline test-ID labels (e.g., `// A1`, `// B3`, `// F1`) are FORBIDDEN. These are ephemeral planning artifacts with no meaning outside the conversation that produced them. If a test needs a description, use an `<summary>` XML doc comment on the method.

```csharp
// ❌ WRONG — region usage
#region Tests on thing A
#endregion

// ❌ WRONG — comment separator
// --- Tests for thing A --------

// ❌ WRONG — inline test-ID label (TUnit)
// B3
[Test]
public async Task SomeTest() { }

// ❌ WRONG — inline test-ID label (xUnit)
// B3
[Fact]
public async Task SomeTest() { }

// ❌ WRONG — section divider comments for helpers
// -------------------------------------------------------------------------
// Helpers
// -------------------------------------------------------------------------
```

## Helper types belong in separate files

When a test file contains private helper types (fake implementations, test doubles, builder helpers), extract them into separate classes in separate files in the same directory. Do NOT use section-divider comments to group them inside the test file.

```csharp
// ❌ WRONG — helpers embedded in the test file with divider comments
// -------------------------------------------------------------------------
// Test infrastructure
// -------------------------------------------------------------------------
private sealed class FakeSink : IDiagnosticsSink { ... }
private sealed class ThrowingSink : IDiagnosticsSink { ... }

// ✅ CORRECT — helpers extracted to separate files in the same directory
// FakeSink.cs
internal sealed class FakeSink : IDiagnosticsSink { ... }

// ThrowingSink.cs
internal sealed class ThrowingSink : IDiagnosticsSink { ... }
```

## No arrange/act/assert comments

- NEVER add `// Arrange`, `// Act`, or `// Assert` comments. 
- Use blank lines to separate logical groups.

## Asynchronous Concerns

- NEVER use `.ConfigureAwait` in tests. Some test harnesses (notably xUnit) install a custom synchronization context that enforces concurrency limits; `.ConfigureAwait(false)` skips that context and can cause deadlocks or limit violations. TUnit doesn't install such a context, but the rule still applies for consistency and to keep tests portable across frameworks.
- NEVER use `Task.Delay` or `Thread.Sleep`. Artificial synchronization makes tests slow and non-deterministic. It is FORBIDDEN.
- NEVER use synchronous waits in async tests. This includes `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`, and any sync wait on a primitive that has an async equivalent — e.g. `ManualResetEventSlim.Wait()`, `CountdownEvent.Wait()`, `SemaphoreSlim.Wait()` (use `WaitAsync` instead). These look "deterministic" because they're real sync primitives rather than artificial timing, but they hold the calling thread-pool thread while waiting. Under concurrent test load other parallel tests starve the holding test of the threads it needs to make progress, and you get false-positive timeouts that look like flakes.
- Poll for observable SUT state using a while/deadline loop with `await Task.Yield()` between checks. This is the right tool when the test is observing the SUT (e.g. "wait until the queue has drained").
- Coordinate between async paths using `TaskCompletionSource<T>` with `TaskCreationOptions.RunContinuationsAsynchronously`, awaited via `tcs.Task.WaitAsync(generousTimeout, cancellationToken)`. The `await` releases the thread back to the pool while waiting; the timeout serves only as a deadlock guard, not as a parallelism check, so make it generous (e.g. 30s). `RunContinuationsAsynchronously` prevents the continuation from running synchronously on whoever calls `SetResult`, which avoids re-entrant deadlocks if the SUT signals from inside a critical section.

  ```csharp
  var allStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
  var startedCount = 0;
  Task DoWork()
  {
      if (Interlocked.Increment(ref startedCount) == expectedCount)
      {
          allStarted.TrySetResult();
      }
      return allStarted.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
  }
  ```

- You MUST use a cancellation token from the test context.
  - NEVER use a `default` or `CancellationToken.None` in tests.
  - It is permissable to make your own cancellation token source, but it must be linked to the test context one.
  - In TUnit, accept a `CancellationToken cancellationToken` parameter on the test method — TUnit injects the test's CT automatically. In xUnit (3.x), assign `TestContext.Current.CancellationToken` ONCE as an instance field; do not re-assign it in every test — that's extra lines for no reason.

## NullLogger

- For `ILogger<T>` dependencies that are not being verified, use `NullLogger<T>.Instance` — NEVER `new Mock<ILogger<T>>()`.
- Tests that are validating logging may use an `ILogger` created from a `MockRepository`.

## Mock setup rules

- Always use a `MockRepository` with `MockBehavior.Strict` — NEVER `new Mock<T>()` directly.
- If using a common dependency injection setup across tests within a class, use a static field to hold the created `IServiceProvider` so it can be shared across tests. In xUnit, assign it in the constructor using `??=` syntax to initialize it once. In TUnit, assign it in a `[Before(Class)] static` hook (or use `[ClassDataSource]` for proper fixture lifetime).
- NEVER add mock setups in the constructor or setup method unless the setup is GUARANTEED to be invoked in EVERY single test in the class.
- Call `mock.Reset()` on any static mock before each test so call records are cleared.
- Per-test setups belong inside the individual test method.
- Exception: property accessors read once during DI service construction may be set up after `Reset()`. Mark them with a comment indicating they are DI-construction requirements.

## Assertions

### Assertion message requirements

In **xUnit**, the following must always include a message parameter:

- `Assert.True`
- `Assert.False`
- `Assert.GreaterThan`
- `Assert.LessThan`
- `Assert.InRange`

Do NOT use them with comparison operators — use dedicated assertion methods instead:

```csharp
// xUnit
// ❌ WRONG — no message
Assert.True(account.IsActive);

// ❌ WRONG — comparison inside Assert.True/False
Assert.True(count == 5, "Expected 5 items");
Assert.True(total > 0, "Expected positive total");

// ✅ CORRECT
Assert.True(account.IsActive, "Expected account to be active after creation");
Assert.Equal(5, count);
Assert.GreaterThan(total, 0, "Expected positive total");
```

In **TUnit**, every `Assert.That(...)` chain accepts a `because` parameter via `.Because("...")`. Always supply one when the value being asserted is a boolean or a relative comparison — the same reasoning as xUnit, just different syntax:

```csharp
// TUnit
// ❌ WRONG — no reason given for boolean assertion
await Assert.That(account.IsActive).IsTrue();

// ❌ WRONG — comparison inside .IsTrue / .IsFalse
await Assert.That(count == 5).IsTrue().Because("Expected 5 items");
await Assert.That(total > 0).IsTrue().Because("Expected positive total");

// ✅ CORRECT
await Assert.That(account.IsActive).IsTrue().Because("Expected account to be active after creation");
await Assert.That(count).IsEqualTo(5);
await Assert.That(total).IsGreaterThan(0).Because("Expected positive total");
```

### Strong Assertions

- NEVER have an assertion that can pass for multiple conditions. Test scenarios MUST be deterministic so you MUST assert the exact expected value.
- NEVER have assertions inside of conditional checks. The ONLY exception is when running multiple scenarios via a parameterised test (TUnit's `[Test] [Arguments(...)]` or xUnit's `[Theory] [InlineData(...)]`) and the conditional is for the scenario type.
- ALWAYS check the count of a collection if you are asserting existince of something in the collection. It is a weak assertion to only check the presence of something without ensuring the state of what you are looking at.
- If you are using a MockRepository, you MUST use `.VerifyAll` so that you do not risk unused setups lingering in test code. This is also why we require strict mock usage.
- Assert something is not called (i.e. like a call to a different system) by using a mock and verifying a setup was NOT called. Moq's `Times.Never` works the same way under both TUnit and xUnit.
- If you are asserting a system was called a certain number of times, you must verify the times called. Otherwise, if it is not the focus of the test, `VerifyAll` will be sufficient.

### Exception assertions

`Record.ExceptionAsync` + `Assert.NotNull` (xUnit) is **FORBIDDEN**. Use the framework's dedicated throws-assertion:

```csharp
// xUnit
await Assert.ThrowsAsync<InvalidOperationException>(
    () => service.DoSomethingAsync(badInput, ct));

// TUnit
await Assert.That(async () => await service.DoSomethingAsync(badInput, ct))
    .Throws<InvalidOperationException>();
```

Always assert the specific exception type — never just "an exception was thrown".

### Try result assertions

- When a method returns `TriedEx<T>` or `TriedNullEx<T?>`, use the dedicated `Assert.TrySucceeded` and `Assert.TryFailed` helpers (these are xUnit-flavored helpers; TUnit users should write the equivalent with `Assert.That(result.Success).IsTrue()` followed by an explicit value assertion until a TUnit-native helper exists).
- **Do NOT use `Assert.True(result.Success)` / `Assert.False(result.Success)` (xUnit) or `Assert.That(result.Success).IsTrue()` (TUnit) without then asserting the resulting value or error.**
- Do NOT access `.Value` without first asserting success.

#### Asserting success — `Assert.TrySucceeded` (xUnit)

```csharp
// xUnit
TriedEx<ThingId> result = await service.TryCreateAsync(input, userId, ct);
var thingId = Assert.TrySucceeded(result, "Expected service to create the thing successfully");
Assert.NotNull(thingId);
```

```csharp
// TUnit (no dedicated helper yet — assert and unwrap explicitly)
TriedEx<ThingId> result = await service.TryCreateAsync(input, userId, ct);
await Assert.That(result.Success).IsTrue().Because("Expected service to create the thing successfully");
var thingId = result.Value;
await Assert.That(thingId).IsNotNull();
```

#### Asserting failure — `Assert.TryFailed` (xUnit)

```csharp
// xUnit
TriedEx<ThingId> result = await service.TryCreateAsync(badInput, userId, ct);
var error = Assert.TryFailed<ThingId, ArgumentException>(result, "Expected validation failure");
Assert.Contains("Name is required", error.Message);
```

```csharp
// TUnit
TriedEx<ThingId> result = await service.TryCreateAsync(badInput, userId, ct);
await Assert.That(result.Success).IsFalse().Because("Expected validation failure");
var error = result.Error;
await Assert.That(error).IsTypeOf<ArgumentException>();
await Assert.That(error.Message).Contains("Name is required");
```

#### Common mistakes

```csharp
// xUnit
// ❌ WRONG — do not use Assert.True/False on Try result types
Assert.True(result.Success);
Assert.False(result.Success);

// ❌ WRONG — do not access .Value without asserting success first
var value = result.Value;

// ✅ CORRECT — use the dedicated helpers
var value = Assert.TrySucceeded(result, "Expected success");
var error = Assert.TryFailed<T, SomeException>(result, "Expected failure");
```

```csharp
// TUnit
// ❌ WRONG — asserting Success without then asserting the value or error
await Assert.That(result.Success).IsTrue();

// ❌ WRONG — accessing .Value before asserting success
var value = result.Value;

// ✅ CORRECT — assert Success, then assert the value or error
await Assert.That(result.Success).IsTrue().Because("Expected success");
await Assert.That(result.Value).IsNotNull();
```

## Code coverage

Do NOT use coverlet (`coverlet.collector`, `coverlet.msbuild`, `coverlet.runsettings`) in this
project — it is incompatible with TUnit. TUnit handles code coverage collection natively.