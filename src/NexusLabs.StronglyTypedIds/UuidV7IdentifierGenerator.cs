namespace NexusLabs.StronglyTypedIds;

/// <summary>
/// Creates UUIDv7-backed strongly typed identifiers using an injected
/// <see cref="TimeProvider"/>.
/// </summary>
/// <typeparam name="TIdentifier">The generated identifier type.</typeparam>
public sealed class UuidV7IdentifierGenerator<TIdentifier> :
    IUuidV7IdentifierGenerator<TIdentifier>
    where TIdentifier : struct, IUuidV7Identifier<TIdentifier>
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a UUIDv7 identifier generator.
    /// </summary>
    /// <param name="timeProvider">The source of UUIDv7 timestamps.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="timeProvider"/> is <see langword="null"/>.
    /// </exception>
    public UuidV7IdentifierGenerator(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public TIdentifier Create() => TIdentifier.Create(_timeProvider);
}
