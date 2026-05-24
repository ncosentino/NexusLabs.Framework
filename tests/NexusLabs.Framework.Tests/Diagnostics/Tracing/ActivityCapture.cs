using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace NexusLabs.Framework.Tests.Diagnostics.Tracing;

/// <summary>
/// Test helper that attaches an <see cref="ActivityListener"/> to capture activities started
/// and stopped on a named <see cref="ActivitySource"/>. Disposed via <c>using</c> in the test.
/// </summary>
internal sealed class ActivityCapture : IDisposable
{
    private readonly ActivityListener _listener;

    public ConcurrentBag<Activity> Started { get; } = new();
    public ConcurrentBag<Activity> Stopped { get; } = new();

    public ActivityCapture(string sourceName)
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStarted = Started.Add,
            ActivityStopped = Stopped.Add,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();
}
