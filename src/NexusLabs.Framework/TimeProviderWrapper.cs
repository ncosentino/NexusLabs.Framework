using System;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLabs.Framework;

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
