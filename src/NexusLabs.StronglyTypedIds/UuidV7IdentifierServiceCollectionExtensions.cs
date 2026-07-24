using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NexusLabs.StronglyTypedIds;

/// <summary>
/// Registers UUIDv7 strongly typed identifier generation services.
/// </summary>
public static class UuidV7IdentifierServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="TimeProvider.System"/> when no time provider exists and
    /// adds the open-generic <see cref="IUuidV7IdentifierGenerator{TIdentifier}"/>
    /// implementation.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddUuidV7IdentifierGeneration(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(
            typeof(IUuidV7IdentifierGenerator<>),
            typeof(UuidV7IdentifierGenerator<>));

        return services;
    }
}
