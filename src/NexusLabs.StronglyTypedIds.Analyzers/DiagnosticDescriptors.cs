using Microsoft.CodeAnalysis;

namespace NexusLabs.StronglyTypedIds.Analyzers;

internal static class DiagnosticDescriptors
{
    private const string HelpLinkBase =
        "https://github.com/ncosentino/NexusLabs.Framework/blob/master/docs/analyzers/";

    public static readonly DiagnosticDescriptor UseUuidV7CreateInsteadOfNew = new(
        id: "NLS0001",
        title: "Use UUIDv7 Create() instead of the generated New() method",
        messageFormat:
            "'{0}.New()' creates a UUIDv4 value even though '{0}' uses the UUIDv7 template; " +
            "replace it with '{0}.Create()'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "The built-in GUID template generates New() with Guid.NewGuid(), which produces UUIDv4. " +
            "UUIDv7-enabled identifiers add Create() as the supported generation API. Construction " +
            "from externally sourced GUID values remains available for rehydration.",
        helpLinkUri: HelpLinkBase + "NLS0001.md");

    public static readonly DiagnosticDescriptor DoNotConstructUuidV7IdFromNewGuid = new(
        id: "NLS0002",
        title: "Use UUIDv7 Create() instead of constructing from Guid.NewGuid()",
        messageFormat:
            "'Guid.NewGuid()' creates UUIDv4 before constructing UUIDv7-enabled '{0}'; " +
            "replace the construction with '{0}.Create()' or pass an externally sourced GUID " +
            "only when intentionally rehydrating an existing identifier",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "Directly passing Guid.NewGuid() to a UUIDv7-enabled identifier constructor bypasses " +
            "the generated UUIDv7 creation API. Arbitrary GUID construction is otherwise allowed " +
            "because persistence and deserialization must be able to rehydrate existing values.",
        helpLinkUri: HelpLinkBase + "NLS0002.md");
}
