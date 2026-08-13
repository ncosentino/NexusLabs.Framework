using Microsoft.CodeAnalysis;

namespace NexusLabs.TUnit.Assertions.Analyzers;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor AssertTriedResultDirectly = new(
        id: "NLT0001",
        title: "Assert Tried results directly",
        messageFormat:
            "Assert the Tried result directly instead of its '{0}' property; " +
            "use `Assert.That(result).Succeeded()` for success/value assertions " +
            "or `Assert.That(result).Failed()` for failure/error assertions",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "TUnit assertions over TriedEx<T> and TriedNullEx<T> should assert the " +
            "complete result. Succeeded() validates and returns the successful value; " +
            "Failed() validates and returns the captured exception.",
        helpLinkUri:
            "https://github.com/ncosentino/NexusLabs.Framework/blob/main/docs/analyzers/NLT0001.md");
}
