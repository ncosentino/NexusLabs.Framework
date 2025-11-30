using System;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLabs.Framework;

public interface ITimeProvider
{
    DateTimeOffset GetUtcNow();

    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}
