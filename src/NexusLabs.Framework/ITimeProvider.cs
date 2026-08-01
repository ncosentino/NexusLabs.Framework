using System;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLabs.Framework;

/// <summary>
/// Wraps a clock and an awaitable delay primitive. This interface predates
/// <see cref="TimeProvider"/> in the BCL (.NET 8+) and is now obsolete.
/// </summary>
/// <remarks>
/// Prefer the BCL's <see cref="TimeProvider"/> directly:
/// <list type="bullet">
///   <item>Production code: inject <see cref="TimeProvider"/> and call <c>GetUtcNow()</c> / <c>Task.Delay(delay, timeProvider, ct)</c>.</item>
///   <item>Tests: install the <c>Microsoft.Extensions.TimeProvider.Testing</c> NuGet package and use <c>FakeTimeProvider</c> for time control.</item>
/// </list>
/// This interface will be removed in the next major version.
/// </remarks>
[Obsolete(
    "ITimeProvider is obsolete. Use System.TimeProvider (BCL .NET 8+) directly. " +
    "For tests, use Microsoft.Extensions.TimeProvider.Testing (FakeTimeProvider). " +
    "ITimeProvider will be removed in the next major version.")]
#pragma warning disable NLF0027 // This is the custom time abstraction the rule exists to prevent. It is already
                               // [Obsolete] and is kept only so consumers can migrate before the next major version.
public interface ITimeProvider
{
    DateTimeOffset GetUtcNow();

    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}
#pragma warning restore NLF0027
