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
}
