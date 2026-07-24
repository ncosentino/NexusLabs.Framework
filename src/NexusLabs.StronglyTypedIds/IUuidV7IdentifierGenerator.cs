namespace NexusLabs.StronglyTypedIds;

/// <summary>
/// Creates UUIDv7-backed values for one strongly typed identifier kind.
/// </summary>
/// <typeparam name="TIdentifier">The generated identifier type.</typeparam>
public interface IUuidV7IdentifierGenerator<TIdentifier>
    where TIdentifier : struct, IUuidV7Identifier<TIdentifier>
{
    /// <summary>
    /// Creates a new UUIDv7-backed identifier.
    /// </summary>
    /// <returns>The newly generated identifier.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The configured time provider returns a timestamp before the Unix epoch.
    /// </exception>
    TIdentifier Create();
}
