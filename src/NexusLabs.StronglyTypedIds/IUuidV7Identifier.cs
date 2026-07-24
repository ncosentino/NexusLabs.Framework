namespace NexusLabs.StronglyTypedIds;

/// <summary>
/// Defines a strongly typed identifier that can create UUIDv7-backed values.
/// </summary>
/// <typeparam name="TSelf">The concrete identifier type.</typeparam>
/// <remarks>
/// This contract describes the identifier's creation capability, not an invariant
/// over every possible value. Parsing, deserialization, and construction from an
/// externally sourced <see cref="Guid"/> may preserve values from other UUID versions.
/// </remarks>
public interface IUuidV7Identifier<TSelf>
    where TSelf : struct, IUuidV7Identifier<TSelf>
{
    /// <summary>
    /// Creates an identifier using a UUIDv7 timestamp obtained from
    /// <paramref name="timeProvider"/>.
    /// </summary>
    /// <param name="timeProvider">The source of the UUIDv7 timestamp.</param>
    /// <returns>A new identifier backed by an RFC 9562 UUIDv7 value.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="timeProvider"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The time provider returns a timestamp before the Unix epoch.
    /// </exception>
    static abstract TSelf Create(TimeProvider timeProvider);
}
