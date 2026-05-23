using System;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLabs.Framework;

/// <summary>
/// Adapts a BCL <see cref="TimeProvider"/> to the obsolete <see cref="ITimeProvider"/> interface.
/// </summary>
/// <remarks>
/// This wrapper is obsolete because <see cref="ITimeProvider"/> itself is obsolete.
/// Inject <see cref="TimeProvider"/> directly and stop using this wrapper. The wrapper
/// will be removed in the next major version alongside <see cref="ITimeProvider"/>.
/// </remarks>
[Obsolete(
    "TimeProviderWrapper is obsolete because ITimeProvider is obsolete. " +
    "Inject System.TimeProvider directly instead. " +
    "TimeProviderWrapper will be removed in the next major version.")]
#pragma warning disable CS0618 // Suppress: this wrapper intentionally adapts an obsolete interface.
public sealed class TimeProviderWrapper(
    TimeProvider _timeProvider) :
    ITimeProvider
{
    public DateTimeOffset GetUtcNow() =>
        _timeProvider.GetUtcNow();

    public async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        await Task.Delay(delay, _timeProvider, cancellationToken);
    }
}
#pragma warning restore CS0618
