using Microsoft.CodeAnalysis;

namespace NexusLabs.Framework.Analyzers;

internal static class DiagnosticDescriptors
{
    private const string UsageCategory = "Usage";

    private const string PerformanceCategory = "Performance";

    private const string HelpLinkBase =
        "https://github.com/ncosentino/NexusLabs.Framework/blob/master/docs/analyzers/";

    public static readonly DiagnosticDescriptor DoNotUseConsoleWrite = new(
        id: "NLF0001",
        title: "Replace Console/Debug.Write with ILogger in library code",
        messageFormat:
            "'{0}' should not be used in library code. " +
            "Inject ILogger and call the appropriate Log* method, " +
            "or accept a callback parameter so the caller controls output sinks.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Console.Write, Console.WriteLine, Debug.Write, and Debug.WriteLine are diagnostic " +
            "output channels of last resort. Library code should route observable output through " +
            "ILogger (or a comparable injectable abstraction) so the consumer controls sinks, " +
            "filtering, and structured logging. Suppress via `dotnet_diagnostic.NLF0001.severity = none` " +
            "in .editorconfig if the project legitimately wants console output (e.g. an entry-point " +
            "executable).",
        helpLinkUri: HelpLinkBase + "NLF0001.md");

    public static readonly DiagnosticDescriptor TryResultValueAccessWithoutSuccessCheck = new(
        id: "NLF0002",
        title: "Check TriedEx.Success before accessing Value",
        messageFormat:
            "Accessing 'Value' on a TriedEx<T>/TriedNullEx<T> when Success is false throws InvalidOperationException. " +
            "Guard with `if (result.Success)` first, " +
            "or use `result.Match(onSuccess, onError)` to handle both branches in a single expression.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "The Value property of TriedEx<T> / TriedNullEx<T> is only valid when Success is true. " +
            "Reading Value when Success is false throws InvalidOperationException at runtime. " +
            "Guard the access via an `if (result.Success)` block, a ternary on `result.Success`, " +
            "short-circuit `result.Success && ...`, or an early return / throw / break / continue " +
            "when Success is false. Promote severity to error via " +
            "`dotnet_diagnostic.NLF0002.severity = error` in .editorconfig to fail the build on " +
            "unguarded access.",
        helpLinkUri: HelpLinkBase + "NLF0002.md");

    public static readonly DiagnosticDescriptor TryResultErrorAccessWithoutSuccessCheck = new(
        id: "NLF0003",
        title: "Check TriedEx.Success is false before accessing Error",
        messageFormat:
            "Accessing 'Error' on a TriedEx<T>/TriedNullEx<T> when Success is true returns null. " +
            "Guard with `if (!result.Success)` first, " +
            "or use `result.Match(onSuccess, onError)` to handle both branches in a single expression.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "The Error property of TriedEx<T> / TriedNullEx<T> is only meaningful when Success is " +
            "false. Reading Error when Success is true returns null and indicates a logic bug in " +
            "the caller. Guard the access via an `if (!result.Success)` block, a ternary, " +
            "short-circuit `!result.Success && ...`, or an early return / throw / break / continue " +
            "when Success is true. Promote severity to error via " +
            "`dotnet_diagnostic.NLF0003.severity = error` in .editorconfig to fail the build on " +
            "unguarded access.",
        helpLinkUri: HelpLinkBase + "NLF0003.md");

    public static readonly DiagnosticDescriptor TryResultErrorNullCheckAfterSuccessCheck = new(
        id: "NLF0004",
        title: "Remove redundant Error null check on Success-false branch",
        messageFormat:
            "After 'Success' has been checked false, 'Error' is guaranteed non-null. " +
            "Remove the redundant `result.Error == null` / `result.Error is null` / `result.Error != null` check.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "TriedEx<T> / TriedNullEx<T> guarantee that Error is non-null whenever Success is false. " +
            "Once a Success-false branch is established (via `if (!result.Success)`, the else of " +
            "`if (result.Success)`, an early return on Success, etc.), additional null checks on " +
            "Error are dead branches and obscure intent. Remove the redundant null check.",
        helpLinkUri: HelpLinkBase + "NLF0004.md");

    public static readonly DiagnosticDescriptor TryResultErrorMustBePreserved = new(
        id: "NLF0005",
        title: "Preserve original Error when returning an exception from a Success-false branch",
        messageFormat:
            "Returning a new exception without referencing 'result.Error' silently drops the underlying failure. " +
            "Return `result.Error` directly, " +
            "wrap it: `new MyException(\"...\", result.Error)`, " +
            "or aggregate it: `new AggregateException(result.Error, ...)`.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "When code in a Success-false branch of a TriedEx<T> / TriedNullEx<T> returns an " +
            "Exception, the original Error must be carried forward. Acceptable forms: return " +
            "`result.Error` directly; wrap in `new MyException(\"...\", result.Error)`; include in " +
            "`new AggregateException(result.Error, ...)`. Returning a fresh exception with no " +
            "reference to the original silently drops the underlying failure and breaks observability.",
        helpLinkUri: HelpLinkBase + "NLF0005.md");

    public static readonly DiagnosticDescriptor MethodWithTryCatchShouldUseTryPattern = new(
        id: "NLF0006",
        title: "Replace whole-body try-catch with Try.Async / Try.GetAsync",
        messageFormat:
            "Async method '{0}' wraps its entire body in a single try-catch. " +
            "Replace with `Try.Async(logger, async () => ...)` (returns `Task<Exception?>`), " +
            "`Try.GetAsync(logger, async () => ...)` (returns `Task<TriedEx<T>>`), " +
            "or `Try.GetOrNullAsync(logger, async () => ...)` (returns `Task<TriedNullEx<T?>>`).",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "An async method whose body is exactly one try-catch statement is the canonical case " +
            "the NexusLabs.Framework.Try helpers exist to abstract. Replacing the manual " +
            "try-catch with Try.Async (for `Task<Exception?>` callers), Try.GetAsync (for " +
            "`Task<TriedEx<T>>` callers), or Try.GetOrNullAsync (for `Task<TriedNullEx<T?>>`) " +
            "centralizes the catch policy and pairs with NLF0002..NLF0005 for safe consumption of " +
            "the result.",
        helpLinkUri: HelpLinkBase + "NLF0006.md");

    public static readonly DiagnosticDescriptor TryAsyncMethodScopeMustProvideLogger = new(
        id: "NLF0007",
        title: "Pass ILogger to method-scoped Try.Async variant",
        messageFormat:
            "Method '{0}' wraps its whole body with a Try.Async variant but does not pass an ILogger. " +
            "Switch to the `(ILogger logger, Func<Task> callback)` overload so caught exceptions are logged. " +
            "The logger-less overloads are for nested or transient usage where the caller already owns the logging context.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "When Try.Async / Try.GetAsync / Try.GetOrNullAsync wraps the entire body of a method, " +
            "the caller benefits from automatic exception logging. Pass an ILogger as the first " +
            "argument so the Try helper can emit a structured error on catch. The logger-less " +
            "overloads are intended for nested or transient usage where the caller already owns " +
            "the logging context.",
        helpLinkUri: HelpLinkBase + "NLF0007.md");

    public static readonly DiagnosticDescriptor ThrowInsideTryAsyncVariant = new(
        id: "NLF0008",
        title: "Return exception from Try.Async callback instead of throwing",
        messageFormat:
            "Method '{0}' throws inside a Try.Async / Try.GetAsync / Try.GetOrNullAsync callback. " +
            "Throw outside the callback, " +
            "or return `new TriedEx<T>(ex)` / `new TriedNullEx<T>(ex)` from the lambda so the Try helper captures the failure as Error.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Try.Async / Try.GetAsync / Try.GetOrNullAsync callbacks are expected to either " +
            "complete normally or return a value that signals failure (e.g. `return new TriedEx<T>(ex)`). " +
            "A bare `throw` inside the callback bypasses the helper's caught-exception path on " +
            "non-Exception derived types and adds an unnecessary unwind. Return the exception " +
            "instead.",
        helpLinkUri: HelpLinkBase + "NLF0008.md");

    public static readonly DiagnosticDescriptor AsyncMethodReturningTryResultShouldUseTryPattern = new(
        id: "NLF0009",
        title: "Wrap async method returning TriedEx<T> with Try.GetAsync",
        messageFormat:
            "Async method '{0}' returns '{1}' but its body is not wrapped with Try.GetAsync / Try.GetOrNullAsync — " +
            "an uncaught exception will fault the Task instead of populating Error. " +
            "Wrap the body: `return await Try.GetAsync(logger, async () => ...)`. " +
            "Direct pass-through (`=> await OtherTryMethod()`) is allowed because the inner method owns the catch.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "An async method whose return type is `Task<TriedEx<T>>` or `Task<TriedNullEx<T>>` is " +
            "promising the caller that it will swallow exceptions into the Error slot — but " +
            "without the Try.GetAsync / Try.GetOrNullAsync wrappers any uncaught exception will " +
            "fault the Task instead. Wrap the method body so the helper supplies the catch policy. " +
            "Direct pass-through (e.g. `=> await OtherMethodReturningTheSameTriedEx()`) is allowed " +
            "since the inner method owns the catch.",
        helpLinkUri: HelpLinkBase + "NLF0009.md");

    public static readonly DiagnosticDescriptor RawStringOpeningQuotesMustBeOnOwnLine = new(
        id: "NLF0010",
        title: "Place opening triple-quote on its own line, aligned with closing",
        messageFormat:
            "The opening triple-quote of a multi-line raw string must be on its own line, " +
            "indented to match the column of the closing triple-quote. " +
            "Move the `\"\"\"` to a new line above the content so opening and closing share the same indent.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "A multi-line raw string literal (`\"\"\"...\"\"\"` or `$\"\"\"...\"\"\"`) strips leading " +
            "whitespace based on the column of the closing `\"\"\"`. When the opening `\"\"\"` dangles " +
            "at the end of an assignment or argument line, the opening and closing tokens visually " +
            "drift apart and the literal's boundaries become harder to scan. Place the opening " +
            "`\"\"\"` on its own line at the same indent as the closing `\"\"\"` so that opening, " +
            "content, and closing all share a single column. Single-line raw strings " +
            "(`var s = \"\"\"value\"\"\";`) are exempt — there is no closing on a separate line to " +
            "align with.",
        helpLinkUri: HelpLinkBase + "NLF0010.md");

    public static readonly DiagnosticDescriptor TriedDisposableValueNotDisposed = new(
        id: "NLF0011",
        title: "Dispose TriedEx/TriedNullEx/Tried that wraps a disposable value",
        messageFormat:
            "Local '{0}' is a {1}<{2}> whose value implements {3} but is never disposed. " +
            "Replace `var {0} = ...` with `using var {0} = ...` (or `await using var {0} = ...`) " +
            "to dispose the wrapped value when the local goes out of scope. " +
            "Returning, passing to another method, or calling `{0}.Dispose()` explicitly also satisfies this rule.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "TriedEx<T>, TriedNullEx<T>, and Tried<T> implement IDisposable and IAsyncDisposable " +
            "so that callers can wrap a disposable T with `using`/`await using` and have the value " +
            "disposed automatically — without first having to check Success. When a local of one of " +
            "these types wraps a disposable T and is dropped on the floor (not used as `using`, not " +
            "returned, not passed to another method, no explicit Dispose), the wrapped value leaks. " +
            "Prefer `using var local = TryDoThing();` so disposal is guaranteed on all exit paths. " +
            "If ownership is genuinely transferred (e.g. cached in a field, registered with a parent), " +
            "suppress with `dotnet_diagnostic.NLF0011.severity = none` at the call site, or pass the " +
            "local to the receiver (the analyzer treats that as ownership transfer).",
        helpLinkUri: HelpLinkBase + "NLF0011.md");

    public static readonly DiagnosticDescriptor StronglyTypedIdParsePatternMisuse = new(
        id: "NLF0013",
        title: "Use the strongly-typed ID's Parse/TryParse instead of constructing from a pre-parsed backing-type value",
        messageFormat:
            "Convert directly via '{0}.{1}' instead of parsing '{2}' separately and constructing '{0}'. " +
            "The strongly-typed ID exposes the same parsing entry points as its backing type.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Types decorated with [StronglyTypedIds.StronglyTypedIdAttribute] generate their own " +
            "Parse/TryParse static methods that mirror the backing type's parsing API across all " +
            "overloads (including IFormatProvider, NumberStyles, etc.). Constructing the ID via " +
            "`new XxxId(BackingType.Parse(s, ...))` or via " +
            "`if (BackingType.TryParse(s, ..., out var v)) { var id = new XxxId(v); }` is an " +
            "awkward round-trip — the strongly-typed ID can be obtained directly with " +
            "`XxxId.Parse(s, ...)` or `if (XxxId.TryParse(s, ..., out var id))`. " +
            "The TryParse flagging is intentionally conservative: it only fires when the TryParse " +
            "call is the entire condition of an `if` statement, the construction is inside the " +
            "success branch, the local is not reassigned in that branch, and no lambda or local " +
            "function boundary separates the two. Both the inline `out var g` form and the older " +
            "predeclared-local `Guid g; if (BackingType.TryParse(s, out g))` form are detected. " +
            "Overload matching is exact: the strongly-typed ID must expose an overload whose " +
            "parameters match the backing-type method's parameters (with `out backingType` " +
            "swapped to `out idType` for TryParse). When the [StronglyTypedId] attribute is not " +
            "visible on a cross-project ID (because it was stripped via [Conditional]), a strict " +
            "structural fallback applies: the ID must be a non-BCL value type defined in a " +
            "different assembly, with a single-parameter public constructor taking the backing " +
            "type and a public instance property of that backing type.",
        helpLinkUri: HelpLinkBase + "NLF0013.md");

    public static readonly DiagnosticDescriptor ParseTryParseMissingFormatProvider = new(
        id: "NLF0014",
        title: "Specify IFormatProvider on Parse/TryParse when a culture-aware overload exists",
        messageFormat:
            "'{0}.{1}' has an overload that accepts an IFormatProvider but none was supplied. " +
            "Pass a culture explicitly (typically 'CultureInfo.InvariantCulture' for machine-readable input) " +
            "to avoid locale-dependent parsing bugs.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Calls to a type's static `Parse` or `TryParse` method without an explicit " +
            "IFormatProvider are silently culture-sensitive: the running thread's CurrentCulture " +
            "drives number formats, decimal separators, date layouts, and other locale-dependent " +
            "behavior. This is a frequent source of bugs that pass on the developer's machine but " +
            "fail in production where the culture differs (or vice versa). When the called type " +
            "exposes a sibling overload of the same name whose parameter list is identical to the " +
            "current call's *plus* an additional IFormatProvider parameter, the explicit overload " +
            "must be used. The analyzer is intentionally stricter than CA1305: there is no " +
            "exclusion for types whose parsing is currently culture-insensitive (e.g. Guid, bool), " +
            "because the discipline of always being explicit is more valuable than the convenience " +
            "of skipping a few calls. Pass `CultureInfo.InvariantCulture` for machine-readable " +
            "input (IDs, configuration values, serialized payloads) or the user's culture for " +
            "human-facing input. Promote severity to error via " +
            "`dotnet_diagnostic.NLF0014.severity = error` in .editorconfig to fail the build on " +
            "implicit-culture parsing.",
        helpLinkUri: HelpLinkBase + "NLF0014.md");

    public static readonly DiagnosticDescriptor TransfersOwnershipInertOnNonDisposable = new(
        id: "NLF0012",
        title: "Parameterless [TransfersOwnership] on non-disposable member is inert",
        messageFormat:
            "'{0}' is annotated with parameterless [TransfersOwnership] but its type is not " +
            "IDisposable/IAsyncDisposable, so the suppressor will never act on it. " +
            "Add target names for conditional ownership (e.g. [TransfersOwnership(nameof(_field))]), " +
            "or move the attribute onto the disposable member itself.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "TransfersOwnershipAttribute has two valid shapes. Shape B is parameterless on a " +
            "disposable field/property/parameter and authorises disposal of that member. Shape A " +
            "takes one or more target names and is placed on a guard flag or parameter — it " +
            "authorises disposal of the listed members inside an `if` whose condition reads the " +
            "annotated flag. A parameterless attribute on a non-disposable member matches neither " +
            "shape and is silently ignored by NLFSUP001. NLF0012 makes that silent footgun " +
            "visible at build time. Fix by either (a) supplying targets for Shape A guard usage, " +
            "or (b) moving the attribute onto the disposable member itself for Shape B.",
        helpLinkUri: HelpLinkBase + "NLF0012.md");

    public static readonly DiagnosticDescriptor TryPrefixedMethodMustReturnTryResultType = new(
        id: "NLF0015",
        title: "Try-prefixed methods must return TriedEx<T>, TriedNullEx<T>, or Exception?",
        messageFormat:
            "Method '{0}' uses the 'Try' prefix but returns '{1}'. " +
            "In this codebase the 'Try' prefix is a contract: it tells callers the method swallows exceptions into a result they must inspect via `.Success` before reading `.Value`. " +
            "Fix by either: (1) CHANGE the return type to TriedEx<T> / TriedNullEx<T> / Exception? (or their Task/ValueTask wrappers) and wrap the body with Try.GetAsync / Try.GetOrNullAsync / Try.Async; OR (2) RENAME the method without the 'Try' prefix (e.g. 'GetByIdAsync' instead of 'TryGetByIdAsync', or 'AcquireOrNullAsync' instead of 'TryAcquireAsync' for null-on-failure patterns) so the name no longer claims a contract the return type does not honour. " +
            "Mixing 'Try' with bool, T?, long?, void, or other non-Try-result returns silently breaks the convention every other Try method follows and forces callers to memorise per-method exception semantics.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "The 'Try' method-name prefix in NexusLabs.Framework is a load-bearing signal: it tells the caller 'this method shields you from exceptions and returns a TriedEx/TriedNullEx/Exception? you must inspect before using the value.' " +
            "Methods that return bool (BCL-style TryParse), T?, long?, or void must NOT use the 'Try' prefix — name them GetXAsync, FindXAsync, AcquireOrNullAsync, or similar instead. " +
            "Allowed return types: TriedEx<T>, Task<TriedEx<T>>, ValueTask<TriedEx<T>>, TriedNullEx<T>, Task<TriedNullEx<T>>, ValueTask<TriedNullEx<T>>, Exception?, Task<Exception?>, ValueTask<Exception?>. " +
            "The analyzer skips overrides and interface implementations because those inherit their name from the base/interface declaration — fix it there, not at every implementation site. " +
            "The static `NexusLabs.Framework.Try` helper class is exempt because its members ARE the convention's infrastructure. " +
            "Names containing an underscore are also skipped: the codebase uses underscore-delimited test naming (`MethodUnderTest_Scenario_Expectation`) where the first segment merely names the SUT and does not itself express an API contract. " +
            "Suppress per-rule via `dotnet_diagnostic.NLF0015.severity = none` in .editorconfig if your codebase uses the BCL TryParse(out T) pattern and does not want to adopt the TriedEx convention.",
        helpLinkUri: HelpLinkBase + "NLF0015.md");

    public static readonly DiagnosticDescriptor HashSetOfStringMustUseOrdinalIgnoreCase = new(
        id: "NLF0016",
        title: "HashSet<string> must use StringComparer.OrdinalIgnoreCase",
        messageFormat:
            "'{0}' produces a HashSet<string> without StringComparer.OrdinalIgnoreCase — `Contains`, `Add`, and `Remove` will treat \"Foo\" and \"foo\" as DIFFERENT keys, which is almost never what string sets are used for (config keys, file names, header names, identifiers from external systems are all case-insensitive in practice). " +
            "Fix: pass StringComparer.OrdinalIgnoreCase explicitly — `new HashSet<string>(StringComparer.OrdinalIgnoreCase)`, `new HashSet<string>(source, StringComparer.OrdinalIgnoreCase)`, or `source.ToHashSet(StringComparer.OrdinalIgnoreCase)`. " +
            "If the set genuinely needs to be case-sensitive (e.g. holding C# identifiers, cryptographic hash strings, or content that is canonically case-distinguishing), suppress at the call site with `#pragma warning disable NLF0016` and a comment explaining why case matters.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "A HashSet<string> created without an explicit comparer (or with any comparer other than StringComparer.OrdinalIgnoreCase — Ordinal, CurrentCulture, CurrentCultureIgnoreCase, InvariantCulture, InvariantCultureIgnoreCase) uses the wrong default for the vast majority of real-world string-set usages. " +
            "Equality on string keys is overwhelmingly about identity-of-meaning rather than identity-of-bytes; \"Authorization\" and \"authorization\" denote the same HTTP header, \"Path/To/File\" and \"path/to/file\" denote the same path on Windows, etc. " +
            "The analyzer fires on both `new HashSet<string>(...)` (any constructor overload, including target-typed `new()`) and `Enumerable.ToHashSet(IEnumerable<string>)` (any overload that does not pass StringComparer.OrdinalIgnoreCase). " +
            "Non-string HashSet<T> is unaffected. ImmutableHashSet<string> is a separate type with different ergonomics and is not covered by this rule. " +
            "Suppress per-call with `#pragma warning disable NLF0016` for genuinely case-sensitive sets (C# identifier names, cryptographic hashes, base64 strings), or per-project with `dotnet_diagnostic.NLF0016.severity = none` in .editorconfig.",
        helpLinkUri: HelpLinkBase + "NLF0016.md");

    public static readonly DiagnosticDescriptor CarterModuleMustBePublicSealedClass = new(
        id: "NLF0017",
        title: "Carter module must be declared 'public sealed class'",
        messageFormat:
            "Carter module '{0}' is declared '{1}' but Carter discovers ICarterModule implementations via reflection over PUBLIC types only — a non-public module compiles cleanly but its routes are silently skipped at startup, returning 404 at runtime with no build error. " +
            "Fix: change the declaration to `public sealed class {0} : ICarterModule`. " +
            "If you genuinely need an abstract base for shared module setup, name it `*CarterModuleBase` and exempt it with `#pragma warning disable NLF0017` — Carter does not register abstract classes either way, so the rule still flags them by default to surface the most common 'concrete module accidentally not registered' bug.",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Carter's reflection-based module discovery enumerates only PUBLIC types implementing `Carter.ICarterModule`. An `internal` module compiles cleanly but is never registered — every route it declares returns 404 at runtime, with no build-time or startup-time error to surface the misconfiguration. This silent failure mode is one of the most common Carter footguns. " +
            "`sealed` is also required by convention because Carter modules are leaf classes — sealing them prevents accidental subclassing that would create duplicate route registrations and downstream ambiguity. " +
            "The analyzer matches any type whose `AllInterfaces` includes `Carter.ICarterModule` (matched by namespace + type name; no Carter package reference required in the analyzer assembly). It fires when the declared accessibility is anything other than `public`, or when the class is not `sealed`, or both. Abstract bases are also flagged — if you intentionally maintain a public abstract `*CarterModuleBase`, suppress it locally with `#pragma warning disable NLF0017` and an inline comment. " +
            "Suppress per-project via `dotnet_diagnostic.NLF0017.severity = none` in .editorconfig if your project does not use Carter or uses a different module-registration mechanism that does not require public sealed types.",
        helpLinkUri: HelpLinkBase + "NLF0017.md");

    public static readonly DiagnosticDescriptor CancellationTokenMustNotHaveDefaultValue = new(
        id: "NLF0018",
        title: "CancellationToken parameters must not have a default value",
        messageFormat:
            "Parameter '{0}' on '{1}.{2}' has a default value ('{3}'). " +
            "An optional `CancellationToken` lets callers silently drop the token, so the downstream I/O ignores cancellation and never stops cleanly on shutdown, request-abort, or timeout. " +
            "Fix: remove the `= {3}` and update every call site to thread an actual token. " +
            "If a particular caller genuinely has no token to thread, pass `CancellationToken.None` EXPLICITLY at the call site — that documents the choice in code instead of hiding it behind an omitted argument. " +
            "If this is a public API on a library that needs ergonomic no-token overloads, suppress per-method with `#pragma warning disable NLF0018` and a comment naming the design intent (e.g. `[EnumeratorCancellation]` IAsyncEnumerable iterators where the BCL convention requires the default).",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Every method that has a `System.Threading.CancellationToken` parameter is held to the rule — there is no suffix-based scope, no kind-based filter (interfaces, classes, structs, records all apply), and no built-in escape hatch beyond `#pragma` at the call site or project-level `severity = none`. " +
            "Making the token optional lets callers omit the argument, which means downstream I/O silently ignores cancellation: requests that should abort on shutdown leak into background tasks, retry loops cannot be stopped, tests that forget the token hang indefinitely on assertion timeouts. The cost of the rule is one extra `CancellationToken.None` token in the few call sites that truly have no token; the benefit is a build-time guarantee that every other call site has consciously chosen which token to pass. " +
            "The analyzer matches `default`, `default(CancellationToken)`, `CancellationToken.None`, and `new CancellationToken()` — any expression whose `EqualsValueClause` evaluates to a CancellationToken's default state. Non-default values on a CancellationToken parameter (e.g. `= someStaticToken`) are unusual but also flagged because the rule is about the OPTIONAL-ness of the parameter, not the specific default value. " +
            "Legitimate exception cases — public framework APIs where the ergonomic-default overload is intentional (e.g. `AcquireAsync(CancellationToken = default)` companion to `AcquireAsync(TimeSpan, CancellationToken)`), or `IAsyncEnumerable` iterators decorated with `[EnumeratorCancellation]` where the BCL convention requires the default — suppress at the call site with `#pragma warning disable NLF0018` and an inline comment. For an entire library project that intentionally exposes optional-token public APIs, opt out via `dotnet_diagnostic.NLF0018.severity = none` in .editorconfig.",
        helpLinkUri: HelpLinkBase + "NLF0018.md");

    public static readonly DiagnosticDescriptor DoNotAllocateEmptyReadOnlyCollection = new(
        id: "NLF0019",
        title: "Return a shared empty collection instead of allocating one for a read-only result",
        messageFormat:
            "'{0}' allocates a fresh empty collection that is only ever exposed as the read-only '{1}', so it can never be mutated through that type — the allocation is wasted on every call. " +
            "Replace it with the shared empty instance: {2}.",
        category: PerformanceCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Allocating a fresh empty collection — of any constructed type — only to expose it through a read-only collection interface " +
            "(IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, IReadOnlyDictionary<TKey,TValue>, IReadOnlySet<T>) hands the caller an object it can never mutate through that interface, " +
            "so a single cached empty instance is always preferable — it removes a per-call heap allocation and is genuinely immutable rather than a mutable instance hidden behind a read-only facade. " +
            "For the list family use the collection expression `[]`, which the compiler lowers to the cached `Array.Empty<T>()` singleton (zero allocation); for IReadOnlyDictionary<TKey,TValue> use `ReadOnlyDictionary<TKey,TValue>.Empty`; for IReadOnlySet<T> use `FrozenSet<T>.Empty`. " +
            "The rule fires only when the created collection is provably empty (no constructor arguments, no initializer or an empty initializer) AND is widened to a read-only interface — so `var x = new List<int>(); Populate(x); return x;` is left alone because the creation's converted type is the mutable List<int>, not the interface. " +
            "The concrete collection type does not matter (List, Dictionary, HashSet, SortedDictionary, ObservableCollection, and user-defined collections all qualify); the read-only interface is the entire signal that the instance is never modified. " +
            "The mutable interfaces (IList<T>, ICollection<T>, ISet<T>, IDictionary<TKey,TValue>) are intentionally NOT covered because their callers may add to the collection, and a fixed-size shared empty would throw on Add. Zero-length array allocations (`new T[0]`) are covered by the built-in CA1825 instead. " +
            "The dictionary and set branches fire only when the corresponding `.Empty` member exists in the compilation (.NET 8+), so the suggested fix is always available on the target framework. " +
            "If a collection type genuinely must be constructed for a constructor side effect (an anti-pattern in its own right), suppress that call site with `#pragma warning disable NLF0019`; otherwise opt out per-project via `dotnet_diagnostic.NLF0019.severity = none` in .editorconfig.",
        helpLinkUri: HelpLinkBase + "NLF0019.md");
}
