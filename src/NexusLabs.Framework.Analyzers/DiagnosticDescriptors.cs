using Microsoft.CodeAnalysis;

namespace NexusLabs.Framework.Analyzers;

internal static class DiagnosticDescriptors
{
    private const string UsageCategory = "Usage";

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
            "`Parse(string)` and `TryParse(string, out T)` static methods that mirror the backing " +
            "type's parsing API. Constructing the ID via `new XxxId(BackingType.Parse(s))` or via " +
            "`if (BackingType.TryParse(s, out var v)) { var id = new XxxId(v); }` is an awkward " +
            "round-trip — the strongly-typed ID can be obtained directly with `XxxId.Parse(s)` or " +
            "`if (XxxId.TryParse(s, out var id))`. The TryParse flagging is intentionally " +
            "conservative: it only fires when the TryParse call is the entire condition of an " +
            "`if` statement, the construction is inside the success branch, the out local is not " +
            "reassigned between declaration and use, and no lambda or local function boundary " +
            "separates the two.",
        helpLinkUri: HelpLinkBase + "NLF0013.md");

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
}
