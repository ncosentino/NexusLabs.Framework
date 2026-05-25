using Microsoft.CodeAnalysis;

namespace NexusLabs.Framework.Analyzers;

internal static class DiagnosticDescriptors
{
    private const string UsageCategory = "Usage";

    public static readonly DiagnosticDescriptor DoNotUseConsoleWrite = new(
        id: "NLF0001",
        title: "Do not use Console.Write / Debug.Write in library code",
        messageFormat: "'{0}' should not be used directly; route output through ILogger or a comparable abstraction",
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
        helpLinkUri: "https://github.com/ncosentino/NexusLabs.Framework/blob/master/CHANGELOG.md");

    public static readonly DiagnosticDescriptor TryResultValueAccessWithoutSuccessCheck = new(
        id: "NLF0002",
        title: "Value property accessed without checking Success first",
        messageFormat: "Accessing 'Value' on TriedEx/TriedNullEx without first checking 'Success' is true",
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
        helpLinkUri: "https://github.com/ncosentino/NexusLabs.Framework/blob/master/CHANGELOG.md");

    public static readonly DiagnosticDescriptor TryResultErrorAccessWithoutSuccessCheck = new(
        id: "NLF0003",
        title: "Error property accessed without checking Success first",
        messageFormat: "Accessing 'Error' on TriedEx/TriedNullEx without first checking 'Success' is false",
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
        helpLinkUri: "https://github.com/ncosentino/NexusLabs.Framework/blob/master/CHANGELOG.md");

    public static readonly DiagnosticDescriptor TryResultErrorNullCheckAfterSuccessCheck = new(
        id: "NLF0004",
        title: "Unnecessary null check on Error after Success has been checked to be false",
        messageFormat: "Null check on 'Error' is unnecessary once 'Success' is known to be false — 'Error' is guaranteed non-null",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "TriedEx<T> / TriedNullEx<T> guarantee that Error is non-null whenever Success is false. " +
            "Once a Success-false branch is established (via `if (!result.Success)`, the else of " +
            "`if (result.Success)`, an early return on Success, etc.), additional null checks on " +
            "Error are dead branches and obscure intent. Remove the redundant null check.",
        helpLinkUri: "https://github.com/ncosentino/NexusLabs.Framework/blob/master/CHANGELOG.md");

    public static readonly DiagnosticDescriptor TryResultErrorMustBePreserved = new(
        id: "NLF0005",
        title: "Original Error must be preserved when returning an exception from a Try-failure branch",
        messageFormat: "When returning an exception in a Success-false branch, return 'Error' directly or include it as inner/aggregated exception to preserve error context",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "When code in a Success-false branch of a TriedEx<T> / TriedNullEx<T> returns an " +
            "Exception, the original Error must be carried forward. Acceptable forms: return " +
            "`result.Error` directly; wrap in `new MyException(\"...\", result.Error)`; include in " +
            "`new AggregateException(result.Error, ...)`. Returning a fresh exception with no " +
            "reference to the original silently drops the underlying failure and breaks observability.",
        helpLinkUri: "https://github.com/ncosentino/NexusLabs.Framework/blob/master/CHANGELOG.md");

    public static readonly DiagnosticDescriptor MethodWithTryCatchShouldUseTryPattern = new(
        id: "NLF0006",
        title: "Async method whose entire body is a single try-catch should use the Try pattern",
        messageFormat: "Async method '{0}' wraps its entire body in try-catch — convert to Try.Async / Try.GetAsync / Try.GetOrNullAsync for consistent error handling",
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
        helpLinkUri: "https://github.com/ncosentino/NexusLabs.Framework/blob/master/CHANGELOG.md");

    public static readonly DiagnosticDescriptor TryAsyncMethodScopeMustProvideLogger = new(
        id: "NLF0007",
        title: "Method-scoped Try.Async variant should receive an ILogger",
        messageFormat: "Method '{0}' uses a Try.Async variant at method scope without an ILogger — use the overload that takes (ILogger, callback)",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "When Try.Async / Try.GetAsync / Try.GetOrNullAsync wraps the entire body of a method, " +
            "the caller benefits from automatic exception logging. Pass an ILogger as the first " +
            "argument so the Try helper can emit a structured error on catch. The logger-less " +
            "overloads are intended for nested or transient usage where the caller already owns " +
            "the logging context.",
        helpLinkUri: "https://github.com/ncosentino/NexusLabs.Framework/blob/master/CHANGELOG.md");

    public static readonly DiagnosticDescriptor ThrowInsideTryAsyncVariant = new(
        id: "NLF0008",
        title: "Do not throw inside a Try.Async variant callback",
        messageFormat: "Method '{0}' throws inside a Try.Async variant callback — return the exception instead so the Try helper can wrap it",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Try.Async / Try.GetAsync / Try.GetOrNullAsync callbacks are expected to either " +
            "complete normally or return a value that signals failure (e.g. `return new TriedEx<T>(ex)`). " +
            "A bare `throw` inside the callback bypasses the helper's caught-exception path on " +
            "non-Exception derived types and adds an unnecessary unwind. Return the exception " +
            "instead.",
        helpLinkUri: "https://github.com/ncosentino/NexusLabs.Framework/blob/master/CHANGELOG.md");
}
