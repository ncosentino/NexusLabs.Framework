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
}
