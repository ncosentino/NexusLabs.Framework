namespace NexusLabs.StronglyTypedIds;

/// <summary>
/// Names the additive templates supplied for GUID-backed strongly typed identifiers.
/// </summary>
public static class GuidIdTemplates
{
    /// <summary>
    /// Adds RFC 9562 UUIDv7 <c>Create</c> methods and UUIDv7 generation contracts.
    /// Warning: this template does not enforce a UUIDv7 value invariant; the generated
    /// GUID constructor still accepts arbitrary values whose provenance and version
    /// must be validated by the caller when that distinction matters.
    /// </summary>
    /// <remarks>
    /// Use this value as an additional template alongside the built-in GUID template.
    /// Parsing, deserialization, <c>default</c>, <c>Empty</c>, and direct constructor
    /// calls may still produce identifiers whose values are not UUIDv7. The bundled
    /// analyzers reject the known UUIDv4 creation paths while preserving construction
    /// from externally sourced GUIDs for rehydration.
    /// </remarks>
    public const string UuidV7 = "NexusLabs.UuidV7";
}
